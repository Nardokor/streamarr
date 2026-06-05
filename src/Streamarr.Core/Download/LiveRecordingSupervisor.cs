using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NLog;
using Streamarr.Common.Disk;
using Streamarr.Core.Channels;
using Streamarr.Core.Configuration;
using Streamarr.Core.Content;
using Streamarr.Core.Download.YtDlp;
using Streamarr.Core.MetadataSource;

namespace Streamarr.Core.Download
{
    // Everything the supervisor needs to (re)launch a single live recording. Built once by the
    // executor and reused for every relaunch attempt so each yt-dlp invocation lands on the same
    // output path and can resume from kept fragments.
    public class LiveRecordingRequest
    {
        public int ContentId { get; set; }
        public string PlatformContentId { get; set; } = string.Empty;
        public PlatformType Platform { get; set; }
        public string Url { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public string CookiesFilePath { get; set; } = string.Empty;
        public string OutputFilename { get; set; } = string.Empty;
        public string MetadataTitle { get; set; } = string.Empty;
        public Action<YtDlpProgress> OnProgress { get; set; }

        // Fired once, on the first attempt only — transitions the content to Recording and emits
        // the LiveStreamStartedEvent. Relaunches must not re-fire it.
        public Action OnStarted { get; set; }
    }

    public interface ILiveRecordingSupervisor
    {
        // Runs the supervised live recording loop and returns the terminal result. The caller
        // (DownloadContentCommandExecutor) treats this exactly like a single Download result.
        YtDlpDownloadResult Supervise(LiveRecordingRequest request);

        // Requests cancellation of an in-progress supervised recording and kills its yt-dlp process.
        void Cancel(int contentId);

        // True while a supervised live recording for this content id is running (including the
        // backoff gap between relaunch attempts, when no yt-dlp process is active).
        bool IsSupervising(int contentId);
    }

    public class LiveRecordingSupervisor : ILiveRecordingSupervisor
    {
        // An attempt that ran at least this long before exiting is treated as having made progress
        // (downloaded fragments), even if it ultimately failed — so a long healthy stream with the
        // occasional blip never accumulates toward the give-up budget. Only rapid back-to-back
        // failures (a genuinely dead/broken stream) exhaust the budget.
        private static readonly TimeSpan HealthyRunThreshold = TimeSpan.FromSeconds(60);

        private readonly IYtDlpClient _ytDlpClient;
        private readonly IMetadataSourceFactory _metadataSourceFactory;
        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly ILiveRetryClock _clock;
        private readonly Logger _logger;

        private readonly ConcurrentDictionary<int, CancellationTokenSource> _supervised = new();

        public LiveRecordingSupervisor(IYtDlpClient ytDlpClient,
                                       IMetadataSourceFactory metadataSourceFactory,
                                       IConfigService configService,
                                       IDiskProvider diskProvider,
                                       ILiveRetryClock clock,
                                       Logger logger)
        {
            _ytDlpClient = ytDlpClient;
            _metadataSourceFactory = metadataSourceFactory;
            _configService = configService;
            _diskProvider = diskProvider;
            _clock = clock;
            _logger = logger;
        }

        private enum LiveProbe
        {
            StillLive,
            Ended,
            GoneFromApi,
            Unknown
        }

        public bool IsSupervising(int contentId) => _supervised.ContainsKey(contentId);

        public void Cancel(int contentId)
        {
            if (_supervised.TryGetValue(contentId, out var cts))
            {
                _logger.Info("Cancelling supervised live recording for content {0}", contentId);
                try
                {
                    cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Loop already finished and disposed the token; nothing to cancel.
                }
            }

            // Always kill any running process, supervised or not, so plain VOD downloads cancel too.
            _ytDlpClient.CancelDownload(contentId);
        }

        public YtDlpDownloadResult Supervise(LiveRecordingRequest request)
        {
            using var cts = new CancellationTokenSource();
            _supervised[request.ContentId] = cts;

            try
            {
                using (_ytDlpClient.AcquireDownloadSlot())
                {
                    return RunLoop(request, cts.Token);
                }
            }
            finally
            {
                _supervised.TryRemove(request.ContentId, out _);
            }
        }

        private YtDlpDownloadResult RunLoop(LiveRecordingRequest request, CancellationToken token)
        {
            var backoff = TimeSpan.FromSeconds(Math.Max(1, _configService.YtDlpLiveRetryBackoffSeconds));
            var maxConsecutiveFailures = Math.Max(1, _configService.YtDlpLiveMaxConsecutiveFailures);
            var maxRetryWindow = TimeSpan.FromMinutes(Math.Max(1, _configService.YtDlpLiveMaxRetryWindowMinutes));

            var consecutiveFailures = 0;
            DateTime? failureWindowStart = null;
            var firstAttempt = true;
            YtDlpDownloadResult last = null;

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    _logger.Info("Live recording for '{0}' cancelled before attempt", request.PlatformContentId);
                    return FinalizeOnStop(last);
                }

                var sawProgress = false;
                Action<YtDlpProgress> progress = p =>
                {
                    sawProgress = true;
                    request.OnProgress?.Invoke(p);
                };

                var startedAt = _clock.UtcNow;

                last = RunAttempt(request, progress, onStarted: firstAttempt ? request.OnStarted : null, keepPartialsOnFailure: true);
                firstAttempt = false;

                if (token.IsCancellationRequested)
                {
                    _logger.Info("Live recording for '{0}' cancelled", request.PlatformContentId);
                    return FinalizeOnStop(last);
                }

                // The platform — not yt-dlp's exit code — is the source of truth for whether the
                // stream has ended. yt-dlp can exit 0 with a *truncated* merged file when a live
                // connection drops, so a "successful" attempt is never accepted as complete until
                // the API confirms the stream is over.
                var probe = ProbeLiveStatus(request);

                if (probe == LiveProbe.Ended || probe == LiveProbe.GoneFromApi)
                {
                    // Stream is over. Accept the file only if this attempt was a clean, complete
                    // capture; if it was interrupted (or never merged), the stream likely ended
                    // during an outage and the file is short — recapture the full archive.
                    if (last.Success && last.IsMergedOutput && !last.WasInterrupted)
                    {
                        _logger.Info("Live recording '{0}' completed cleanly", request.PlatformContentId);
                        return last;
                    }

                    _logger.Info(
                        "Stream '{0}' ended but the capture looks incomplete (success={1}, merged={2}, interrupted={3}); recapturing full archive",
                        request.PlatformContentId,
                        last.Success,
                        last.IsMergedOutput,
                        last.WasInterrupted);

                    return RecaptureComplete(request, last);
                }

                // Stream still live, or the API could not be reached. If it is unreachable but we
                // have a clean merged file, accept it as a best effort rather than looping forever.
                if (probe == LiveProbe.Unknown && last.Success && last.IsMergedOutput && !last.WasInterrupted)
                {
                    _logger.Warn(
                        "Could not confirm live status for '{0}'; accepting the clean merged capture",
                        request.PlatformContentId);
                    return last;
                }

                // We must resume. Count this toward the give-up budget unless the attempt made
                // progress (a long run, download progress, or a — truncated — successful capture).
                var madeProgress = last.Success || sawProgress || (_clock.UtcNow - startedAt) >= HealthyRunThreshold;
                if (madeProgress)
                {
                    consecutiveFailures = 0;
                    failureWindowStart = null;
                }

                failureWindowStart ??= _clock.UtcNow;
                consecutiveFailures++;

                if (consecutiveFailures > maxConsecutiveFailures ||
                    (_clock.UtcNow - failureWindowStart.Value) > maxRetryWindow)
                {
                    _logger.Warn(
                        "Giving up live recording '{0}' after {1} consecutive failure(s) over {2:n0}s",
                        request.PlatformContentId,
                        consecutiveFailures,
                        (_clock.UtcNow - failureWindowStart.Value).TotalSeconds);
                    return last ?? Failed();
                }

                // A "successful" interrupted attempt left a truncated merged file at the output
                // path; remove it so the relaunch re-captures instead of short-circuiting on
                // "[download] has already been downloaded".
                DeleteIfExists(last);

                _logger.Info(
                    "Live recording '{0}' interrupted (probe={1}); relaunching in {2:n0}s (failure {3}/{4})",
                    request.PlatformContentId,
                    probe,
                    backoff.TotalSeconds,
                    consecutiveFailures,
                    maxConsecutiveFailures);

                _clock.Wait(backoff, token);
            }
        }

        // The stream has ended but the most recent capture is incomplete. Discard the truncated
        // output and do one clean pass to fetch the full archive (now available as a VOD).
        private YtDlpDownloadResult RecaptureComplete(LiveRecordingRequest request, YtDlpDownloadResult last)
        {
            DeleteIfExists(last);

            var result = RunAttempt(request, request.OnProgress, onStarted: null, keepPartialsOnFailure: false);

            if (result.Success)
            {
                return result;
            }

            // Couldn't recapture (archive not yet available, etc.) — fall back to whatever we had.
            return last != null && last.Success ? last : result;
        }

        private YtDlpDownloadResult RunAttempt(LiveRecordingRequest request, Action<YtDlpProgress> onProgress, Action onStarted, bool keepPartialsOnFailure)
        {
            return _ytDlpClient.DownloadHeld(
                request.ContentId,
                request.Url,
                request.OutputPath,
                isLive: true,
                cookiesFilePath: string.IsNullOrEmpty(request.CookiesFilePath) ? null : request.CookiesFilePath,
                onProgress: onProgress,
                onStarted: onStarted,
                outputFilename: string.IsNullOrEmpty(request.OutputFilename) ? null : request.OutputFilename,
                metadataTitle: string.IsNullOrEmpty(request.MetadataTitle) ? null : request.MetadataTitle,
                keepPartialsOnFailure: keepPartialsOnFailure);
        }

        private void DeleteIfExists(YtDlpDownloadResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.FilePath))
            {
                return;
            }

            try
            {
                if (_diskProvider.FileExists(result.FilePath))
                {
                    _diskProvider.DeleteFile(result.FilePath);
                    _logger.Debug("Removed truncated capture before relaunch: {0}", result.FilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to remove truncated capture {0}", result.FilePath);
            }
        }

        // On user-requested cancellation, keep a finished file if we somehow have one, else fail
        // (the executor will set the content back to Missing rather than the transient Recording).
        private YtDlpDownloadResult FinalizeOnStop(YtDlpDownloadResult last)
        {
            if (last != null && last.Success && last.IsMergedOutput)
            {
                return last;
            }

            return Failed();
        }

        private static YtDlpDownloadResult Failed() =>
            new YtDlpDownloadResult { Success = false, ErrorMessage = "Live recording could not be completed" };

        private LiveProbe ProbeLiveStatus(LiveRecordingRequest request)
        {
            try
            {
                var source = ResolveSource(request.Platform);
                if (source == null)
                {
                    return LiveProbe.Unknown;
                }

                var updates = source
                    .GetLivestreamStatusUpdates(new[] { request.PlatformContentId })
                    .ToDictionary(u => u.PlatformContentId);

                if (!updates.TryGetValue(request.PlatformContentId, out var update) || !update.ExistsOnPlatform)
                {
                    return LiveProbe.GoneFromApi;
                }

                return update.NewContentType switch
                {
                    ContentType.Live => LiveProbe.StillLive,
                    ContentType.Upcoming => LiveProbe.StillLive,
                    ContentType.Vod => LiveProbe.Ended,
                    _ => LiveProbe.Unknown
                };
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Live status probe failed for '{0}'; treating as still live", request.PlatformContentId);
                return LiveProbe.Unknown;
            }
        }

        // Mirror LivestreamStatusService: a channel's platform may delegate live-status checks to a
        // different platform's source (e.g. Fourthwall hosting unlisted YouTube videos).
        private IMetadataSource ResolveSource(PlatformType platform)
        {
            var channelSource = _metadataSourceFactory.GetByPlatform(platform);
            if (channelSource == null)
            {
                return null;
            }

            var delegatePlatform = channelSource.LivestreamDelegatePlatform;
            if (delegatePlatform != platform)
            {
                return _metadataSourceFactory.GetByPlatform(delegatePlatform) ?? channelSource;
            }

            return channelSource;
        }
    }
}
