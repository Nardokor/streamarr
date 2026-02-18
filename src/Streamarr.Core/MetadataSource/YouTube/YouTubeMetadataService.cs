using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Download.YtDlp;

namespace Streamarr.Core.MetadataSource.YouTube
{
    public class YouTubeMetadataService : ICreatorMetadataService
    {
        private readonly IYtDlpClient _ytDlpClient;
        private readonly Logger _logger;

        public YouTubeMetadataService(IYtDlpClient ytDlpClient, Logger logger)
        {
            _ytDlpClient = ytDlpClient;
            _logger = logger;
        }

        public CreatorMetadataResult SearchCreator(string query)
        {
            var channelUrl = NormalizeChannelUrl(query);

            _logger.Info("Searching for creator: {0} (resolved to {1})", query, channelUrl);

            var channelInfo = _ytDlpClient.GetChannelInfo(channelUrl);

            var channelName = !string.IsNullOrWhiteSpace(channelInfo.Channel)
                ? channelInfo.Channel
                : channelInfo.Uploader;

            var channelId = !string.IsNullOrWhiteSpace(channelInfo.ChannelId)
                ? channelInfo.ChannelId
                : channelInfo.UploaderId;

            var channelPageUrl = !string.IsNullOrWhiteSpace(channelInfo.ChannelUrl)
                ? channelInfo.ChannelUrl
                : channelInfo.UploaderUrl;

            return new CreatorMetadataResult
            {
                Name = channelName,
                Description = channelInfo.Description,
                ThumbnailUrl = channelInfo.Thumbnail,
                Channels = new List<ChannelMetadataResult>
                {
                    new ChannelMetadataResult
                    {
                        Platform = PlatformType.YouTube,
                        PlatformId = channelId,
                        PlatformUrl = channelPageUrl,
                        Title = channelName,
                        Description = channelInfo.Description,
                        ThumbnailUrl = channelInfo.Thumbnail
                    }
                }
            };
        }

        public ChannelMetadataResult GetChannelMetadata(string platformUrl)
        {
            var channelInfo = _ytDlpClient.GetChannelInfo(platformUrl);

            var channelName = !string.IsNullOrWhiteSpace(channelInfo.Channel)
                ? channelInfo.Channel
                : channelInfo.Uploader;

            var channelId = !string.IsNullOrWhiteSpace(channelInfo.ChannelId)
                ? channelInfo.ChannelId
                : channelInfo.UploaderId;

            var channelPageUrl = !string.IsNullOrWhiteSpace(channelInfo.ChannelUrl)
                ? channelInfo.ChannelUrl
                : channelInfo.UploaderUrl;

            return new ChannelMetadataResult
            {
                Platform = PlatformType.YouTube,
                PlatformId = channelId,
                PlatformUrl = channelPageUrl,
                Title = channelName,
                Description = channelInfo.Description,
                ThumbnailUrl = channelInfo.Thumbnail
            };
        }

        public List<ContentMetadataResult> GetNewContent(string platformUrl, DateTime? since = null)
        {
            var dateAfter = since?.ToString("yyyyMMdd");
            var videos = _ytDlpClient.GetChannelVideos(platformUrl, dateAfter: dateAfter);

            _logger.Info("Found {0} content items for {1}", videos.Count, platformUrl);

            return videos.Select(MapToContentMetadata).ToList();
        }

        private static ContentMetadataResult MapToContentMetadata(YtDlpVideoInfo video)
        {
            return new ContentMetadataResult
            {
                PlatformContentId = video.Id,
                ContentType = DetermineContentType(video),
                Title = video.Title,
                Description = video.Description,
                ThumbnailUrl = video.Thumbnail,
                Duration = video.Duration.HasValue ? TimeSpan.FromSeconds(video.Duration.Value) : null,
                AirDateUtc = ParseUploadDate(video.UploadDate)
            };
        }

        private static ContentType DetermineContentType(YtDlpVideoInfo video)
        {
            if (video.WasLive == true || video.IsLive == true)
            {
                return ContentType.Livestream;
            }

            if (video.Duration.HasValue && video.Duration.Value <= 60)
            {
                return ContentType.Short;
            }

            return ContentType.Video;
        }

        private static DateTime? ParseUploadDate(string uploadDate)
        {
            if (string.IsNullOrWhiteSpace(uploadDate))
            {
                return null;
            }

            if (DateTime.TryParseExact(uploadDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
            {
                return date;
            }

            return null;
        }

        private static string NormalizeChannelUrl(string query)
        {
            if (query.StartsWith("http://") || query.StartsWith("https://"))
            {
                return query;
            }

            if (query.StartsWith("@"))
            {
                return $"https://www.youtube.com/{query}";
            }

            if (query.StartsWith("UC") && query.Length == 24)
            {
                return $"https://www.youtube.com/channel/{query}";
            }

            return $"https://www.youtube.com/@{query}";
        }
    }
}
