using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NLog;
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

        // Re-probing the live status on every relaunch would burn an API call per blip; cache the
        // last probe briefly so a rapid fail/relaunch storm reuses it.
        private static readonly TimeSpan ProbeCacheTtl = TimeSpan.FromSeconds(30);

        private readonly IYtDlpClient _ytDlpClient;
        private readonly IMetadataSourceFactory _metadataSourceFactory;
        private readonly IConfigService _configService;
        private readonly ILiveRetryClock _clock;
        private readonly Logger _logger;

        private readonly ConcurrentDictionary<int, CancellationTokenSource> _supervised = new();

        public LiveRecordingSupervisor(IYtDlpClient ytDlpClient,
                                       IMetadataSourceFactory metadataSourceFactory,
                                       IConfigService configService,
                                       ILiveRetryClock clock,
                                       Logger logger)
        {
            _ytDlpClient = ytDlpClient;
            _metadataSourceFactory = metadataSourceFactory;
            _configService = configService;
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

            var probeAt = DateTime.MinValue;
            var cachedProbe = LiveProbe.Unknown;

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

                last = _ytDlpClient.DownloadHeld(
                    request.ContentId,
                    request.Url,
                    request.OutputPath,
                    isLive: true,
                    cookiesFilePath: string.IsNullOrEmpty(request.CookiesFilePath) ? null : request.CookiesFilePath,
                    onProgress: progress,
                    onStarted: firstAttempt ? request.OnStarted : null,
                    outputFilename: string.IsNullOrEmpty(request.OutputFilename) ? null : request.OutputFilename,
                    metadataTitle: string.IsNullOrEmpty(request.MetadataTitle) ? null : request.MetadataTitle,
                    keepPartialsOnFailure: true);

                firstAttempt = false;

                // A merged final file means yt-dlp ran to completion — the stream ended cleanly.
                if (last.Success && last.IsMergedOutput)
                {
                    _logger.Info("Live recording '{0}' completed cleanly", request.PlatformContentId);
                    return last;
                }

                if (token.IsCancellationRequested)
                {
                    _logger.Info("Live recording for '{0}' cancelled", request.PlatformContentId);
                    return FinalizeOnStop(last);
                }

                var madeProgress = sawProgress || (_clock.UtcNow - startedAt) >= HealthyRunThreshold;
                if (madeProgress)
                {
                    consecutiveFailures = 0;
                    failureWindowStart = null;
                }

                // Decide whether the stream is still going before retrying.
                if (_clock.UtcNow - probeAt >= ProbeCacheTtl)
                {
                    cachedProbe = ProbeLiveStatus(request);
                    probeAt = _clock.UtcNow;
                }

                if (cachedProbe == LiveProbe.Ended || cachedProbe == LiveProbe.GoneFromApi)
                {
                    _logger.Info(
                        "Stream '{0}' is no longer live ({1}); finalizing recording",
                        request.PlatformContentId,
                        cachedProbe);
                    return Finalize(request, last);
                }

                // Blip or Unknown — relaunch if the failure budget allows.
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

                _logger.Info(
                    "Live recording '{0}' interrupted (still live); relaunching in {1:n0}s (failure {2}/{3})",
                    request.PlatformContentId,
                    backoff.TotalSeconds,
                    consecutiveFailures,
                    maxConsecutiveFailures);

                _clock.Wait(backoff, token);
            }
        }

        // The stream ended while we were between/inside attempts. If we already have a complete
        // merged file, use it; otherwise do one final clean invocation to merge the kept fragments
        // (or grab the freshly-archived VOD), this time allowing normal partial cleanup.
        private YtDlpDownloadResult Finalize(LiveRecordingRequest request, YtDlpDownloadResult last)
        {
            if (last != null && last.Success && last.IsMergedOutput)
            {
                return last;
            }

            _logger.Debug("Running final merge attempt for '{0}'", request.PlatformContentId);

            var result = _ytDlpClient.DownloadHeld(
                request.ContentId,
                request.Url,
                request.OutputPath,
                isLive: true,
                cookiesFilePath: string.IsNullOrEmpty(request.CookiesFilePath) ? null : request.CookiesFilePath,
                onProgress: request.OnProgress,
                onStarted: null,
                outputFilename: string.IsNullOrEmpty(request.OutputFilename) ? null : request.OutputFilename,
                metadataTitle: string.IsNullOrEmpty(request.MetadataTitle) ? null : request.MetadataTitle,
                keepPartialsOnFailure: false);

            if (result.Success)
            {
                return result;
            }

            // Fall back to whatever the last attempt produced, if anything usable.
            return last != null && last.Success ? last : result;
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
