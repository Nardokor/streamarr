using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using Streamarr.Common.Disk;
using Streamarr.Common.Processes;

namespace Streamarr.Core.Download
{
    public interface IAudioVideoMuxService
    {
        // Wraps an audio-only file with a static image to produce a Plex-playable video.
        // Returns the new .mp4 path on success, null if skipped or failed.
        string WrapAudioWithImage(string audioFilePath, string imageFilePath);
    }

    public class AudioVideoMuxService : IAudioVideoMuxService
    {
        private static readonly HashSet<string> AudioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".wav", ".mp3", ".m4a", ".flac", ".ogg", ".aac"
        };

        private readonly IProcessProvider _processProvider;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public AudioVideoMuxService(IProcessProvider processProvider, IDiskProvider diskProvider, Logger logger)
        {
            _processProvider = processProvider;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public string WrapAudioWithImage(string audioFilePath, string imageFilePath)
        {
            if (!AudioExtensions.Contains(Path.GetExtension(audioFilePath)))
            {
                return null;
            }

            if (!_diskProvider.FileExists(imageFilePath))
            {
                _logger.Debug("Skipping audio-video mux: avatar not found at {0}", imageFilePath);
                return null;
            }

            var outputPath = Path.ChangeExtension(audioFilePath, ".mp4");

            _logger.Debug("Wrapping audio '{0}' with static image into '{1}'",
                Path.GetFileName(audioFilePath), Path.GetFileName(outputPath));

            // Scale image to 1280x720 with letterboxing, encode to H.264 at 1fps.
            // ultrafast + stillimage produces a near-zero-size video track quickly.
            var args = string.Join(" ", new[]
            {
                "-y",
                "-loop 1 -framerate 1",
                $"-i \"{imageFilePath}\"",
                $"-i \"{audioFilePath}\"",
                "-vf \"scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,setsar=1\"",
                "-c:v libx264 -preset ultrafast -tune stillimage -pix_fmt yuv420p",
                "-c:a aac -b:a 256k",
                "-movflags +faststart",
                "-shortest",
                $"\"{outputPath}\""
            });

            var result = _processProvider.StartAndCapture("ffmpeg", args);

            if (result.ExitCode != 0)
            {
                _logger.Warn("ffmpeg failed wrapping audio with image (exit {0})", result.ExitCode);
                return null;
            }

            try
            {
                _diskProvider.DeleteFile(audioFilePath);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Could not delete original audio file after mux: {0}", audioFilePath);
            }

            _logger.Debug("Audio wrapped successfully: {0}", outputPath);
            return outputPath;
        }
    }
}
