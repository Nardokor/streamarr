using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NLog;
using Streamarr.Common.Disk;
using Streamarr.Common.Processes;

namespace Streamarr.Core.Download.YtDlp
{
    public interface IYtDlpClient
    {
        YtDlpDownloadResult Download(string url, string outputPath, Action<YtDlpProgress> onProgress = null);
        YtDlpChannelInfo GetChannelInfo(string channelUrl);
        List<YtDlpVideoInfo> GetChannelVideos(string channelUrl, int? limit = null, string dateAfter = null);
        YtDlpVideoInfo GetVideoInfo(string videoUrl);
        string GetVersion();
        bool IsAvailable();
    }

    public class YtDlpClient : IYtDlpClient
    {
        private static readonly Regex ProgressRegex = new Regex(
            @"\[download\]\s+(?<percent>[\d.]+)%\s+of\s+~?\s*(?<total>[\d.]+\w+)\s+at\s+(?<speed>[\d.]+\w+/s)\s+ETA\s+(?<eta>\S+)",
            RegexOptions.Compiled);

        private static readonly Regex DestinationRegex = new Regex(
            @"\[(?:Merger|download)\]\s+(?:Destination:\s+)?(.+\.(?:mp4|mkv|webm|mov|avi|flv|m4a|mp3|opus|ogg|wav))",
            RegexOptions.Compiled);

        private static readonly Regex AlreadyDownloadedRegex = new Regex(
            @"\[download\]\s+(.+)\s+has already been downloaded",
            RegexOptions.Compiled);

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IProcessProvider _processProvider;
        private readonly IDiskProvider _diskProvider;
        private readonly YtDlpSettings _settings;
        private readonly Logger _logger;

        public YtDlpClient(IProcessProvider processProvider,
                           IDiskProvider diskProvider,
                           Logger logger)
        {
            _processProvider = processProvider;
            _diskProvider = diskProvider;
            _settings = new YtDlpSettings();
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

        public string GetVersion()
        {
            var output = _processProvider.StartAndCapture(_settings.BinaryPath, "--version");

            if (output.ExitCode != 0)
            {
                throw new InvalidOperationException("yt-dlp not found or returned an error");
            }

            return output.Lines.Count > 0 ? output.Lines[0].Content.Trim() : string.Empty;
        }

        public YtDlpVideoInfo GetVideoInfo(string videoUrl)
        {
            _logger.Debug("Getting video info: {0}", videoUrl);

            var args = BuildMetadataArgs(videoUrl);
            var output = _processProvider.StartAndCapture(_settings.BinaryPath, args);

            if (output.ExitCode != 0)
            {
                var error = string.Join(Environment.NewLine, output.Error.Select(l => l.Content));
                throw new InvalidOperationException($"yt-dlp failed to get video info: {error}");
            }

            var json = string.Join(string.Empty, output.Standard.Select(l => l.Content));

            return JsonSerializer.Deserialize<YtDlpVideoInfo>(json, JsonOptions);
        }

        public YtDlpChannelInfo GetChannelInfo(string channelUrl)
        {
            _logger.Debug("Getting channel info: {0}", channelUrl);

            var args = $"--dump-single-json --flat-playlist --skip-download --playlist-end 1 {Quote(channelUrl)}";
            var output = _processProvider.StartAndCapture(_settings.BinaryPath, args);

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

            argParts.Add(Quote(url));

            var args = string.Join(" ", argParts);
            var output = _processProvider.StartAndCapture(_settings.BinaryPath, args);

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

        public YtDlpDownloadResult Download(string url, string outputPath, Action<YtDlpProgress> onProgress = null)
        {
            _diskProvider.EnsureFolder(outputPath);

            var args = BuildDownloadArgs(url, outputPath);
            var outputFile = string.Empty;
            var errors = new List<string>();

            _logger.Info("Starting yt-dlp download: {0}", url);
            _logger.Debug("yt-dlp args: {0}", args);

            var process = _processProvider.Start(
                _settings.BinaryPath,
                args,
                null,
                line =>
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        return;
                    }

                    _logger.Trace("yt-dlp: {0}", line);

                    var destMatch = DestinationRegex.Match(line);
                    if (destMatch.Success)
                    {
                        outputFile = destMatch.Groups[1].Value.Trim();
                    }

                    var alreadyMatch = AlreadyDownloadedRegex.Match(line);
                    if (alreadyMatch.Success)
                    {
                        outputFile = alreadyMatch.Groups[1].Value.Trim();
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

            _processProvider.WaitForExit(process);

            var exitCode = process.ExitCode;
            var success = exitCode == 0 && !string.IsNullOrEmpty(outputFile);
            long fileSize = 0;

            if (success && _diskProvider.FileExists(outputFile))
            {
                fileSize = _diskProvider.GetFileSize(outputFile);
                _logger.Info("Download complete: {0} ({1} bytes)", outputFile, fileSize);
            }
            else if (exitCode == 0 && string.IsNullOrEmpty(outputFile))
            {
                _logger.Warn("yt-dlp exited successfully but no output file was detected");
                success = false;
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

        private static string BuildMetadataArgs(string url)
        {
            return $"--dump-json --skip-download --socket-timeout 15 {Quote(url)}";
        }

        private string BuildDownloadArgs(string url, string outputPath)
        {
            var args = new List<string>
            {
                "--newline",
                "--no-playlist",
                "-f", Quote(_settings.PreferredFormat),
                "-o", Quote(Path.Combine(outputPath, "%(title)s [%(id)s].%(ext)s"))
            };

            if (_settings.EmbedMetadata)
            {
                args.Add("--embed-metadata");
            }

            if (_settings.EmbedThumbnail)
            {
                args.Add("--embed-thumbnail");
            }

            if (!string.IsNullOrWhiteSpace(_settings.CookieFilePath))
            {
                args.Add("--cookies");
                args.Add(Quote(_settings.CookieFilePath));
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
