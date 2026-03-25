using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NLog;
using Streamarr.Common.Disk;
using Streamarr.Common.Processes;
using Streamarr.Core.Configuration;

namespace Streamarr.Core.Download.YtDlp
{
    public interface IYtDlpClient
    {
        YtDlpDownloadResult Download(int contentId, string url, string outputPath, bool isLive = false, bool needsCookies = false, Action<YtDlpProgress> onProgress = null);
        void CancelDownload(int contentId);
        YtDlpChannelInfo GetChannelInfo(string channelUrl);
        List<YtDlpVideoInfo> GetChannelVideos(string channelUrl, int? limit = null, string dateAfter = null);
        List<YtDlpVideoInfo> GetMembershipTabVideos(string channelUrl);
        YtDlpVideoInfo GetVideoInfo(string videoUrl);

        /// <summary>
        /// Lightweight accessibility check using --print (no format resolution, no deno).
        /// Returns true if the video is accessible with current cookies; false if it requires
        /// a membership level the cookies don't satisfy.
        /// </summary>
        bool IsVideoAccessible(string videoId);

        bool IsDenoAvailable();

        string GetVersion();
        bool IsAvailable();
        string SelfUpdate();
        bool HasCookies { get; }
    }

    public class YtDlpClient : IYtDlpClient
    {
        private static readonly Regex ProgressRegex = new Regex(
            @"\[download\]\s+(?<percent>[\d.]+)%\s+of\s+~?\s*(?<total>[\d.]+\w+)\s+at\s+(?<speed>[\d.]+\w+/s)\s+ETA\s+(?<eta>\S+)",
            RegexOptions.Compiled);

        // [download] Destination: /path/to/file.ext
        private static readonly Regex DownloadDestinationRegex = new Regex(
            @"\[download\]\s+Destination:\s+(.+\.(?:mp4|mkv|webm|mov|avi|flv|m4a|mp3|opus|ogg|wav|webp|jpg|jpeg|png))",
            RegexOptions.Compiled);

        // [Merger] Merging formats into "/path/to/file.ext"
        private static readonly Regex MergerDestinationRegex = new Regex(
            @"\[Merger\]\s+Merging formats into\s+""(.+\.(?:mp4|mkv|webm|mov|avi|flv|m4a|mp3|opus|ogg|wav))""",
            RegexOptions.Compiled);

        private static readonly Regex AlreadyDownloadedRegex = new Regex(
            @"\[download\]\s+(.+)\s+has already been downloaded",
            RegexOptions.Compiled);

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ConcurrentDictionary<int, Process> _activeDownloads = new();

        private readonly IProcessProvider _processProvider;
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public bool HasCookies => !string.IsNullOrWhiteSpace(Settings.CookieFilePath);

        private string CookieArg => !string.IsNullOrWhiteSpace(Settings.CookieFilePath)
            ? $" --cookies {Quote(Settings.CookieFilePath)}"
            : string.Empty;

        private YtDlpSettings Settings => new YtDlpSettings
        {
            BinaryPath = _configService.YtDlpBinaryPath,
            TempDownloadFolder = _configService.YtDlpTempDownloadFolder,
            CookieFilePath = _configService.YtDlpCookieFilePath,
            EmbedMetadata = _configService.YtDlpEmbedMetadata,
            EmbedThumbnail = _configService.YtDlpEmbedThumbnail,
            PreferredFormat = _configService.YtDlpPreferredFormat,
            MaxConcurrentDownloads = _configService.YtDlpMaxConcurrentDownloads,
            DenoBinaryPath = _configService.YtDlpDenoBinaryPath,
        };

        /// <summary>
        /// Returns an environment dictionary that prepends the deno binary's directory to PATH
        /// so yt-dlp can discover it. Returns null when deno is expected to be in PATH already.
        /// </summary>
        private System.Collections.Specialized.StringDictionary BuildDenoEnvironment()
        {
            var denoPath = Settings.DenoBinaryPath;
            if (string.IsNullOrWhiteSpace(denoPath) ||
                string.Equals(denoPath, "deno", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var denoDir = Path.GetDirectoryName(denoPath);
            if (string.IsNullOrWhiteSpace(denoDir))
            {
                return null;
            }

            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var env = new System.Collections.Specialized.StringDictionary();
            env["PATH"] = $"{denoDir}{Path.PathSeparator}{currentPath}";
            return env;
        }

        public YtDlpClient(IProcessProvider processProvider,
                           IDiskProvider diskProvider,
                           IConfigService configService,
                           Logger logger)
        {
            _processProvider = processProvider;
            _diskProvider = diskProvider;
            _configService = configService;
            _logger = logger;
        }

        public bool IsAvailable()
        {
            try
            {
                var version = GetVersion();
                return !string.IsNullOrWhiteSpace(version);
            }
            catch
            {
                return false;
            }
        }

        public bool IsDenoAvailable()
        {
            try
            {
                var denoPath = !string.IsNullOrWhiteSpace(Settings.DenoBinaryPath)
                    ? Settings.DenoBinaryPath
                    : "deno";
                var output = _processProvider.StartAndCapture(denoPath, "--version");
                return output.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public string GetVersion()
        {
            var output = _processProvider.StartAndCapture(Settings.BinaryPath, "--version");

            if (output.ExitCode != 0)
            {
                throw new InvalidOperationException("yt-dlp not found or returned an error");
            }

            return output.Lines.Count > 0 ? output.Lines[0].Content.Trim() : string.Empty;
        }

        public string SelfUpdate()
        {
            // --update-to nightly pulls from the yt-dlp nightly release channel.
            // If the binary is not writable (e.g. running as non-root in Docker),
            // this will throw and the caller should handle gracefully.
            var output = _processProvider.StartAndCapture(Settings.BinaryPath, "--update-to nightly");
            var combined = string.Join('\n', System.Linq.Enumerable.Select(output.Lines, l => l.Content)).Trim();

            if (output.ExitCode != 0)
            {
                throw new InvalidOperationException($"yt-dlp --update-to nightly failed: {combined}");
            }

            return combined;
        }

        public YtDlpVideoInfo GetVideoInfo(string videoUrl)
        {
            _logger.Debug("Getting video info: {0}", videoUrl);

            var args = BuildMetadataArgs(videoUrl);
            var output = _processProvider.StartAndCapture(Settings.BinaryPath, args, BuildDenoEnvironment());

            if (output.ExitCode != 0)
            {
                var error = string.Join(Environment.NewLine, output.Error.Select(l => l.Content));
                throw new InvalidOperationException($"yt-dlp failed to get video info: {error}");
            }

            var json = string.Join(string.Empty, output.Standard.Select(l => l.Content));

            return JsonSerializer.Deserialize<YtDlpVideoInfo>(json, JsonOptions);
        }

        public bool IsVideoAccessible(string videoId)
        {
            var url = $"https://www.youtube.com/watch?v={videoId}";

            // --print availability reads from video metadata — does not require format
            // resolution or trigger the n-challenge. We parse the printed value to
            // distinguish "members_only"/"subscriber_only" from genuinely public content.
            var args = $"--print availability --no-playlist --socket-timeout 15{CookieArg} {Quote(url)}";
            var output = _processProvider.StartAndCapture(Settings.BinaryPath, args, BuildDenoEnvironment());

            if (output.ExitCode != 0)
            {
                var error = string.Join(" ", output.Error.Select(l => l.Content));
                var lower = error.ToLowerInvariant();

                var isAccessDenial = lower.Contains("members only") ||
                                     lower.Contains("members-only") ||
                                     lower.Contains("join this channel") ||
                                     lower.Contains("requires subscription") ||
                                     lower.Contains("private video") ||
                                     lower.Contains("video unavailable");

                if (isAccessDenial)
                {
                    _logger.Debug("IsVideoAccessible({0}) → inaccessible (exit {1}): {2}", videoId, output.ExitCode, error.Split('\n')[0].Trim());
                    return false;
                }

                // Non-access failure (network, solver, etc.) — assume accessible to avoid
                // incorrectly blocking content when yt-dlp encounters a transient error.
                _logger.Warn("IsVideoAccessible({0}) — non-access error (treating as accessible): {1}", videoId, error.Split('\n')[0].Trim());
                return true;
            }

            // Parse the printed availability value. Exit 0 with members_only/subscriber_only
            // means yt-dlp could reach the metadata but the content is gated.
            var availability = output.Standard.FirstOrDefault()?.Content?.Trim().ToLowerInvariant() ?? "public";
            var inaccessible = availability is "members_only" or "subscriber_only"
                                             or "premium_only" or "needs_auth" or "private";

            _logger.Debug("IsVideoAccessible({0}) → availability={1}, accessible={2}", videoId, availability, !inaccessible);
            return !inaccessible;
        }

        public YtDlpChannelInfo GetChannelInfo(string channelUrl)
        {
            _logger.Debug("Getting channel info: {0}", channelUrl);

            var args = $"--dump-single-json --flat-playlist --skip-download --playlist-end 1 --socket-timeout 30{CookieArg} {Quote(channelUrl)}";
            var output = _processProvider.StartAndCapture(Settings.BinaryPath, args, BuildDenoEnvironment());

            if (output.ExitCode != 0)
            {
                var error = string.Join(Environment.NewLine, output.Error.Select(l => l.Content));
                throw new InvalidOperationException($"yt-dlp failed to get channel info: {error}");
            }

            var json = string.Join(string.Empty, output.Standard.Select(l => l.Content));

            return JsonSerializer.Deserialize<YtDlpChannelInfo>(json, JsonOptions);
        }

        public List<YtDlpVideoInfo> GetChannelVideos(string channelUrl, int? limit = null, string dateAfter = null)
        {
            _logger.Debug("Getting channel content: {0} (limit={1}, dateAfter={2})", channelUrl, limit, dateAfter);

            // Strip any existing tab suffix so we can append our own
            var baseUrl = channelUrl.TrimEnd('/');
            foreach (var knownTab in new[] { "/videos", "/shorts", "/streams", "/live" })
            {
                if (baseUrl.EndsWith(knownTab, StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = baseUrl[..^knownTab.Length];
                    break;
                }
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allVideos = new List<YtDlpVideoInfo>();

            foreach (var tab in new[] { "videos", "shorts", "streams" })
            {
                var tabUrl = $"{baseUrl}/{tab}";
                var items = FetchFromTab(tabUrl, limit, dateAfter);
                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item.Id) && seen.Add(item.Id))
                    {
                        allVideos.Add(item);
                    }
                }
            }

            _logger.Debug("Found {0} total content items for channel {1}", allVideos.Count, channelUrl);

            return allVideos;
        }

        public List<YtDlpVideoInfo> GetMembershipTabVideos(string channelUrl)
        {
            var baseUrl = channelUrl.TrimEnd('/');
            foreach (var knownTab in new[] { "/videos", "/shorts", "/streams", "/live", "/membership" })
            {
                if (baseUrl.EndsWith(knownTab, StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = baseUrl[..^knownTab.Length];
                    break;
                }
            }

            // Fetch all (no dateAfter) so historical members content can be backfilled
            return FetchFromTab($"{baseUrl}/membership", limit: null, dateAfter: null);
        }

        private List<YtDlpVideoInfo> FetchFromTab(string url, int? limit, string dateAfter)
        {
            _logger.Debug("Fetching tab: {0}", url);

            var argParts = new List<string>
            {
                "--flat-playlist",
                "--dump-json",
                "--skip-download"
            };

            if (limit.HasValue)
            {
                argParts.Add($"--playlist-end {limit.Value}");
            }

            if (!string.IsNullOrWhiteSpace(dateAfter))
            {
                argParts.Add($"--dateafter {dateAfter}");
            }

            if (!string.IsNullOrWhiteSpace(Settings.CookieFilePath))
            {
                argParts.Add($"--cookies {Quote(Settings.CookieFilePath)}");
            }

            argParts.Add(Quote(url));

            var args = string.Join(" ", argParts);
            var output = _processProvider.StartAndCapture(Settings.BinaryPath, args, BuildDenoEnvironment());

            if (output.ExitCode != 0)
            {
                var error = string.Join(Environment.NewLine, output.Error.Select(l => l.Content));
                _logger.Debug("yt-dlp returned non-zero for {0} (tab may be empty): {1}", url, error);
                return new List<YtDlpVideoInfo>();
            }

            var videos = new List<YtDlpVideoInfo>();

            foreach (var line in output.Standard)
            {
                var trimmed = line.Content?.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                try
                {
                    var video = JsonSerializer.Deserialize<YtDlpVideoInfo>(trimmed, JsonOptions);
                    if (video != null)
                    {
                        videos.Add(video);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.Warn(ex, "Failed to parse video JSON line from {0}", url);
                }
            }

            _logger.Debug("Found {0} items from {1}", videos.Count, url);
            return videos;
        }

        public YtDlpDownloadResult Download(int contentId, string url, string outputPath, bool isLive = false, bool needsCookies = false, Action<YtDlpProgress> onProgress = null)
        {
            _diskProvider.EnsureFolder(outputPath);

            var args = BuildDownloadArgs(url, outputPath, isLive, needsCookies);
            var mergedFile = string.Empty;
            var fragmentFiles = new List<string>();
            var alreadyDownloadedFile = string.Empty;
            var errors = new List<string>();

            _logger.Info("Starting yt-dlp download: {0}", url);
            _logger.Debug("yt-dlp args: {0}", args);

            var process = _processProvider.Start(
                Settings.BinaryPath,
                args,
                BuildDenoEnvironment(),
                line =>
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        return;
                    }

                    _logger.Trace("yt-dlp: {0}", line);

                    var dlMatch = DownloadDestinationRegex.Match(line);
                    if (dlMatch.Success)
                    {
                        var dest = dlMatch.Groups[1].Value.Trim();
                        if (!fragmentFiles.Contains(dest))
                        {
                            fragmentFiles.Add(dest);
                        }
                    }

                    var mergeMatch = MergerDestinationRegex.Match(line);
                    if (mergeMatch.Success)
                    {
                        mergedFile = mergeMatch.Groups[1].Value.Trim();
                    }

                    var alreadyMatch = AlreadyDownloadedRegex.Match(line);
                    if (alreadyMatch.Success)
                    {
                        alreadyDownloadedFile = alreadyMatch.Groups[1].Value.Trim();
                    }

                    if (onProgress != null)
                    {
                        var progress = ParseProgress(line);
                        if (progress != null)
                        {
                            onProgress(progress);
                        }
                    }
                },
                line =>
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _logger.Warn("yt-dlp stderr: {0}", line);
                        errors.Add(line);
                    }
                });

            _activeDownloads[contentId] = process;
            try
            {
                _processProvider.WaitForExit(process);
            }
            finally
            {
                _activeDownloads.TryRemove(contentId, out _);
            }

            // Determine the final output file: merged > already-downloaded > last fragment
            var outputFile = !string.IsNullOrEmpty(mergedFile) ? mergedFile
                           : !string.IsNullOrEmpty(alreadyDownloadedFile) ? alreadyDownloadedFile
                           : fragmentFiles.LastOrDefault() ?? string.Empty;

            var exitCode = process.ExitCode;
            var success = exitCode == 0 && !string.IsNullOrEmpty(outputFile);
            long fileSize = 0;

            if (success && _diskProvider.FileExists(outputFile))
            {
                fileSize = _diskProvider.GetFileSize(outputFile);
                _logger.Info("Download complete: {0} ({1} bytes)", outputFile, fileSize);

                // For live recordings with -k, validate the merge and clean up fragments
                if (isLive && !string.IsNullOrEmpty(mergedFile) && fragmentFiles.Count > 0)
                {
                    CleanUpLiveFragments(mergedFile, fragmentFiles, fileSize);
                }
            }
            else if (exitCode == 0 && string.IsNullOrEmpty(outputFile))
            {
                _logger.Warn("yt-dlp exited successfully but no output file was detected");
                success = false;
            }

            if (!success)
            {
                CleanUpPartialFiles(fragmentFiles);
            }

            return new YtDlpDownloadResult
            {
                Success = success,
                FilePath = outputFile,
                FileSize = fileSize,
                ExitCode = exitCode,
                ErrorMessage = success ? string.Empty : string.Join(Environment.NewLine, errors)
            };
        }

        public void CancelDownload(int contentId)
        {
            if (_activeDownloads.TryGetValue(contentId, out var process))
            {
                _logger.Info("Cancelling download for content {0} (PID {1})", contentId, process.Id);

                try
                {
                    // Kill the entire process tree so child processes (e.g. ffmpeg spawned
                    // during live recording) are also terminated. Without this, ffmpeg keeps
                    // the stdout/stderr pipe open and WaitForExit() never returns.
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to kill yt-dlp process for content {0}", contentId);
                }
            }
            else
            {
                _logger.Debug("No active download found for content {0}", contentId);
            }
        }

        private void CleanUpLiveFragments(string mergedFile, List<string> fragmentFiles, long mergedSize)
        {
            var fragmentTotal = fragmentFiles
                .Where(f => _diskProvider.FileExists(f))
                .Sum(f => _diskProvider.GetFileSize(f));

            if (fragmentTotal == 0)
            {
                _logger.Debug("No fragment files found on disk; skipping cleanup");
                return;
            }

            var ratio = (double)mergedSize / fragmentTotal;
            _logger.Debug(
                "Live merge check — merged: {0} bytes, fragments total: {1} bytes, ratio: {2:P1}",
                mergedSize,
                fragmentTotal,
                ratio);

            if (ratio >= 0.85)
            {
                foreach (var fragment in fragmentFiles)
                {
                    if (_diskProvider.FileExists(fragment))
                    {
                        _diskProvider.DeleteFile(fragment);
                        _logger.Debug("Deleted fragment: {0}", fragment);
                    }
                }

                _logger.Info("Cleaned up {0} fragment file(s) after live merge verification", fragmentFiles.Count);
            }
            else
            {
                _logger.Warn(
                    "Merged file is only {0:P0} of fragment total — possible failed merge; keeping fragments for manual inspection",
                    ratio);
            }
        }

        private void CleanUpPartialFiles(List<string> fragmentFiles)
        {
            foreach (var file in fragmentFiles)
            {
                foreach (var candidate in new[] { file, file + ".part" })
                {
                    if (_diskProvider.FileExists(candidate))
                    {
                        try
                        {
                            _diskProvider.DeleteFile(candidate);
                            _logger.Debug("Deleted partial file: {0}", candidate);
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn(ex, "Failed to delete partial file: {0}", candidate);
                        }
                    }
                }
            }
        }

        private string BuildMetadataArgs(string url)
        {
            return $"--dump-json --skip-download --socket-timeout 15{CookieArg} {Quote(url)}";
        }

        private string BuildDownloadArgs(string url, string outputPath, bool isLive = false, bool needsCookies = false)
        {
            var args = new List<string>
            {
                "--newline",
                "--no-playlist",
                "-f", Quote(Settings.PreferredFormat),
                "-o", Quote(Path.Combine(outputPath, "%(title)s [%(id)s].%(ext)s"))
            };

            if (isLive)
            {
                args.Add("-k");
                args.Add("--live-from-start");
                args.Add("--hls-use-mpegts");
                args.Add("--wait-for-video 5-30");
                args.Add("--fragment-retries 15");
                args.Add("--skip-unavailable-fragments");
                args.Add("--retry-sleep fragment:5");
                args.Add("--socket-timeout 30");
            }

            if (Settings.EmbedMetadata)
            {
                args.Add("--embed-metadata");
            }

            if (Settings.EmbedThumbnail)
            {
                args.Add("--embed-thumbnail");
            }

            if (needsCookies && !string.IsNullOrWhiteSpace(Settings.CookieFilePath))
            {
                args.Add("--cookies");
                args.Add(Quote(Settings.CookieFilePath));
            }

            args.Add(Quote(url));

            return string.Join(" ", args);
        }

        private static YtDlpProgress ParseProgress(string line)
        {
            var match = ProgressRegex.Match(line);
            if (!match.Success)
            {
                return null;
            }

            double.TryParse(match.Groups["percent"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent);

            return new YtDlpProgress
            {
                Status = "downloading",
                PercentComplete = percent,
                Speed = match.Groups["speed"].Value,
                Eta = match.Groups["eta"].Value
            };
        }

        private static string Quote(string value)
        {
            return $"\"{value}\"";
        }
    }
}
