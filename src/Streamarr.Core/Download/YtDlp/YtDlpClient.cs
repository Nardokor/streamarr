using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NLog;
using Streamarr.Common.Disk;
using Streamarr.Common.Processes;

namespace Streamarr.Core.Download.YtDlp
{
    public interface IYtDlpClient
    {
        YtDlpDownloadResult Download(string url, string outputPath, Action<YtDlpProgress> onProgress = null);
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
