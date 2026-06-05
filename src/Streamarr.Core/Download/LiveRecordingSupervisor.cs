using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

            // Captured data is never thrown away across relaunches: yt-dlp keeps the fragments (-k),
            // and the truncated merged file is moved aside to a sidecar (not deleted). We hold on to
            // the largest capture seen so an interrupted recording — especially one interrupted near
            // the end — cannot be lost.
            var state = new PreservationState();

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    _logger.Info("Live recording for '{0}' cancelled before attempt", request.PlatformContentId);
                    return BestEffort(request, last, state);
                }

                var sawProgress = false;
                Action<YtDlpProgress> progress = p =>
                {
                    sawProgress = true;
                    request.OnProgress?.Invoke(p);
                };

                var startedAt = _clock.UtcNow;

                last = RunAttempt(request, progress, onStarted: firstAttempt ? request.OnStarted : null, keepPartialsOnFailure: true, keepFragments: true);
                firstAttempt = false;

                if (token.IsCancellationRequested)
                {
                    _logger.Info("Live recording for '{0}' cancelled", request.PlatformContentId);
                    return BestEffort(request, last, state);
                }

                // The platform — not yt-dlp's exit code — is the source of truth for whether the
                // stream has ended. yt-dlp can exit 0 with a *truncated* merged file when a live
                // connection drops, so a "successful" attempt is never accepted as complete until
                // the API confirms the stream is over.
                var probe = ProbeLiveStatus(request);

                if (probe == LiveProbe.Ended || probe == LiveProbe.GoneFromApi)
                {
                    return FinalizeEnded(request, last, state);
                }

                // Stream still live, or the API could not be reached. If it is unreachable but we
                // have a clean merged file and nothing held aside, accept it rather than loop forever.
                if (probe == LiveProbe.Unknown && IsCleanCapture(last) && !state.HasPreserved)
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
                    return BestEffort(request, last, state);
                }

                // Move the (truncated) merged file aside — keeping the largest one — and free the
                // output path so the relaunch resumes from the kept fragments instead of
                // short-circuiting on "[download] has already been downloaded". Fragments stay put.
                Preserve(last, state);

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

        // The platform reports the stream has ended. Produce the most complete file we can without
        // losing anything we already captured.
        private YtDlpDownloadResult FinalizeEnded(LiveRecordingRequest request, YtDlpDownloadResult last, PreservationState state)
        {
            // Ran straight through to a clean end with nothing held aside — accept it, just sweep
            // the kept (-k) fragments.
            if (IsCleanCapture(last) && !state.HasPreserved)
            {
                _logger.Info("Live recording '{0}' completed cleanly", request.PlatformContentId);
                CleanUpResidualFiles(last.FilePath);
                return last;
            }

            // Fold the last attempt into the preserved set (frees the output path), then do one final
            // pass that resumes from the kept fragments and — without -k — lets yt-dlp clean them up.
            Preserve(last, state);

            _logger.Info("Stream '{0}' ended; running final merge pass to complete the recording", request.PlatformContentId);
            var final = RunAttempt(request, request.OnProgress, onStarted: null, keepPartialsOnFailure: false, keepFragments: false);

            return Reconcile(request, final, state);
        }

        // Keep the largest of {final capture, preserved sidecar} at the canonical output path and
        // remove the loser. Guarantees the most complete file survives even if the final pass failed.
        private YtDlpDownloadResult Reconcile(LiveRecordingRequest request, YtDlpDownloadResult final, PreservationState state)
        {
            var finalPath = final != null && final.Success && !string.IsNullOrEmpty(final.FilePath) && _diskProvider.FileExists(final.FilePath)
                ? final.FilePath
                : null;
            var finalSize = finalPath != null ? _diskProvider.GetFileSize(finalPath) : -1;

            var preservedPath = state.HasPreserved && _diskProvider.FileExists(state.PreservedFile) ? state.PreservedFile : null;
            var preservedSize = preservedPath != null ? _diskProvider.GetFileSize(preservedPath) : -1;

            var authoritative = finalSize >= preservedSize ? finalPath : preservedPath;

            if (authoritative == null)
            {
                _logger.Warn("Live recording '{0}' ended but produced no usable file", request.PlatformContentId);
                return final ?? Failed();
            }

            var target = state.CanonicalOutput ?? authoritative;
            if (!string.Equals(authoritative, target, StringComparison.Ordinal))
            {
                try
                {
                    _diskProvider.MoveFile(authoritative, target, overwrite: true);
                    authoritative = target;
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to move recovered capture {0} -> {1}; keeping it in place", authoritative, target);
                }
            }

            if (preservedPath != null && !string.Equals(preservedPath, authoritative, StringComparison.Ordinal) && _diskProvider.FileExists(preservedPath))
            {
                TryDelete(preservedPath);
            }

            CleanUpResidualFiles(authoritative);

            var size = _diskProvider.FileExists(authoritative) ? _diskProvider.GetFileSize(authoritative) : 0;
            _logger.Info("Live recording '{0}' finalized: {1} ({2} bytes)", request.PlatformContentId, authoritative, size);

            return new YtDlpDownloadResult { Success = true, FilePath = authoritative, FileSize = size, IsMergedOutput = true };
        }

        // Recording stopped before a confirmed end (cancelled or budget exhausted). Keep the best
        // partial we captured rather than losing it; fragments are left in place so a later retry
        // can still resume.
        private YtDlpDownloadResult BestEffort(LiveRecordingRequest request, YtDlpDownloadResult last, PreservationState state)
        {
            Preserve(last, state);

            var preservedPath = state.HasPreserved && _diskProvider.FileExists(state.PreservedFile) ? state.PreservedFile : null;
            if (preservedPath == null)
            {
                return last != null && last.Success ? last : Failed();
            }

            var target = state.CanonicalOutput ?? preservedPath;
            if (!string.Equals(preservedPath, target, StringComparison.Ordinal))
            {
                try
                {
                    _diskProvider.MoveFile(preservedPath, target, overwrite: true);
                    preservedPath = target;
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to restore preserved capture {0} -> {1}", preservedPath, target);
                }
            }

            var size = _diskProvider.FileExists(preservedPath) ? _diskProvider.GetFileSize(preservedPath) : 0;
            _logger.Info("Live recording '{0}' stopped; keeping best partial capture {1} ({2} bytes)", request.PlatformContentId, preservedPath, size);

            return new YtDlpDownloadResult { Success = true, FilePath = preservedPath, FileSize = size, IsMergedOutput = true };
        }

        private static bool IsCleanCapture(YtDlpDownloadResult r) =>
            r != null && r.Success && r.IsMergedOutput && !r.WasInterrupted;

        // Moves a truncated-but-successful merged file aside to a sidecar, keeping only the largest
        // capture seen. The output path is left free for the next yt-dlp run to resume into.
        private void Preserve(YtDlpDownloadResult result, PreservationState state)
        {
            if (result == null || !result.Success || string.IsNullOrEmpty(result.FilePath))
            {
                return;
            }

            try
            {
                if (!_diskProvider.FileExists(result.FilePath))
                {
                    return;
                }

                state.CanonicalOutput ??= result.FilePath;
                var size = _diskProvider.GetFileSize(result.FilePath);

                if (state.HasPreserved && size <= state.PreservedSize)
                {
                    // We already hold an equal-or-larger capture; just free the output path.
                    TryDelete(result.FilePath);
                    return;
                }

                var sidecar = SidecarPath(result.FilePath);
                _diskProvider.MoveFile(result.FilePath, sidecar, overwrite: true);
                state.PreservedFile = sidecar;
                state.PreservedSize = size;
                _logger.Debug("Preserved interrupted capture aside: {0} ({1} bytes)", sidecar, size);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to preserve interrupted capture {0}", result.FilePath);
            }
        }

        // Removes yt-dlp's leftover fragments / per-format intermediates that share the output's base
        // name. Safe: only siblings beginning with "<base>." that look like intermediates are touched.
        private void CleanUpResidualFiles(string finalOutputPath)
        {
            if (string.IsNullOrEmpty(finalOutputPath))
            {
                return;
            }

            try
            {
                var dir = Path.GetDirectoryName(finalOutputPath);
                if (string.IsNullOrEmpty(dir) || !_diskProvider.FolderExists(dir))
                {
                    return;
                }

                var prefix = Path.GetFileNameWithoutExtension(finalOutputPath) + ".";
                if (prefix.Length <= 1)
                {
                    return;
                }

                foreach (var file in _diskProvider.GetFiles(dir, recursive: false))
                {
                    if (string.Equals(file, finalOutputPath, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileName(file);
                    if (fileName.StartsWith(prefix, StringComparison.Ordinal) && IsIntermediate(fileName))
                    {
                        TryDelete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Residual fragment sweep failed for {0}", finalOutputPath);
            }
        }

        private void TryDelete(string path)
        {
            try
            {
                if (_diskProvider.FileExists(path))
                {
                    _diskProvider.DeleteFile(path);
                    _logger.Debug("Removed residual file {0}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to remove residual file {0}", path);
            }
        }

        private static readonly Regex FormatFragmentRegex = new Regex(@"\.f\d+\.", RegexOptions.Compiled);

        private static bool IsIntermediate(string fileName) =>
            fileName.Contains(".part", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("-Frag", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".temp", StringComparison.OrdinalIgnoreCase) ||
            FormatFragmentRegex.IsMatch(fileName);

        private static string SidecarPath(string outputFile)
        {
            var dir = Path.GetDirectoryName(outputFile) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(outputFile);
            return Path.Combine(dir, name + ".recovering.tmp");
        }

        private YtDlpDownloadResult RunAttempt(LiveRecordingRequest request, Action<YtDlpProgress> onProgress, Action onStarted, bool keepPartialsOnFailure, bool keepFragments)
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
                keepPartialsOnFailure: keepPartialsOnFailure,
                keepFragments: keepFragments);
        }

        private sealed class PreservationState
        {
            public string CanonicalOutput { get; set; }

            public string PreservedFile { get; set; }

            public long PreservedSize { get; set; }

            public bool HasPreserved => !string.IsNullOrEmpty(PreservedFile);
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
