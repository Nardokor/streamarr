using System;
using System.IO;
using FFMpegCore;
using NLog;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Common.Extensions;

namespace Streamarr.Core.MediaFiles
{
    public interface IVideoThumbnailService
    {
        string GenerateThumbnail(int contentId, string videoFilePath);
    }

    public class VideoThumbnailService : IVideoThumbnailService
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly Logger _logger;

        public VideoThumbnailService(IAppFolderInfo appFolderInfo, Logger logger)
        {
            _appFolderInfo = appFolderInfo;
            _logger = logger;
        }

        public string GenerateThumbnail(int contentId, string videoFilePath)
        {
            if (!File.Exists(videoFilePath))
            {
                _logger.Warn("Cannot generate thumbnail — file not found: {0}", videoFilePath);
                return string.Empty;
            }

            var outputDir = Path.Combine(_appFolderInfo.GetMediaCoverPath(), "Content", contentId.ToString());
            var outputPath = Path.Combine(outputDir, "thumbnail.jpg");

            try
            {
                Directory.CreateDirectory(outputDir);

                var mediaInfo = FFProbe.Analyse(videoFilePath);
                var duration = mediaInfo.Duration;

                // Capture at 10% of duration, minimum 5 seconds in, capped at 60s
                var captureSeconds = Math.Min(Math.Max(duration.TotalSeconds * 0.1, 5), 60);
                var captureTime = TimeSpan.FromSeconds(captureSeconds);

                FFMpeg.Snapshot(videoFilePath, outputPath, captureTime: captureTime);

                _logger.Debug("Generated thumbnail for content {0} at {1}", contentId, outputPath);
                return $"/MediaCover/Content/{contentId}/thumbnail.jpg";
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to generate thumbnail for content {0} from '{1}'", contentId, videoFilePath);
                return string.Empty;
            }
        }
    }
}
