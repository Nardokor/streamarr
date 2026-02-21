using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Download.YtDlp;

namespace Streamarr.Core.MetadataSource.YouTube
{
    public class YouTubeMetadataService : ICreatorMetadataService
    {
        // Full YouTube URL (youtube.com or youtu.be)
        private static readonly Regex YouTubeUrlRegex = new Regex(
            @"^https?://(www\.)?(youtube\.com|youtu\.be)/",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // @handle — starts with @ and has no whitespace
        private static readonly Regex HandleRegex = new Regex(
            @"^@\S+$",
            RegexOptions.Compiled);

        // YouTube channel ID: "UC" followed by exactly 22 base64url chars
        private static readonly Regex ChannelIdRegex = new Regex(
            @"^UC[\w\-]{22}$",
            RegexOptions.Compiled);

        // Bare handle: only word chars, dots, hyphens — no spaces, no slashes
        private static readonly Regex BareHandleRegex = new Regex(
            @"^[\w.\-]+$",
            RegexOptions.Compiled);

        private readonly IYtDlpClient _ytDlpClient;
        private readonly Logger _logger;

        public YouTubeMetadataService(IYtDlpClient ytDlpClient, Logger logger)
        {
            _ytDlpClient = ytDlpClient;
            _logger = logger;
        }

        public CreatorMetadataResult SearchCreator(string query)
        {
            query = query.Trim();

            YtDlpChannelInfo channelInfo;

            if (YouTubeUrlRegex.IsMatch(query))
            {
                _logger.Info("Lookup by URL: {0}", query);
                channelInfo = _ytDlpClient.GetChannelInfo(query);
            }
            else if (HandleRegex.IsMatch(query))
            {
                var url = $"https://www.youtube.com/{query}";
                _logger.Info("Lookup by handle: {0}", url);
                channelInfo = _ytDlpClient.GetChannelInfo(url);
            }
            else if (ChannelIdRegex.IsMatch(query))
            {
                var url = $"https://www.youtube.com/channel/{query}";
                _logger.Info("Lookup by channel ID: {0}", url);
                channelInfo = _ytDlpClient.GetChannelInfo(url);
            }
            else if (BareHandleRegex.IsMatch(query))
            {
                var url = $"https://www.youtube.com/@{query}";
                _logger.Info("Lookup by bare handle: {0}", url);
                try
                {
                    channelInfo = _ytDlpClient.GetChannelInfo(url);
                }
                catch (Exception ex)
                {
                    _logger.Info("Bare handle lookup failed ({0}), falling back to name search", ex.Message);
                    channelInfo = SearchAndResolveChannel(query);
                }
            }
            else
            {
                _logger.Info("No URL pattern matched, falling back to name search: {0}", query);
                channelInfo = SearchAndResolveChannel(query);
            }

            return BuildResult(channelInfo);
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
            var flatVideos = _ytDlpClient.GetChannelVideos(platformUrl, dateAfter: dateAfter);

            _logger.Info("Found {0} content items in flat listing for {1}", flatVideos.Count, platformUrl);

            var results = new List<ContentMetadataResult>();

            foreach (var flat in flatVideos)
            {
                var videoUrl = !string.IsNullOrWhiteSpace(flat.WebpageUrl)
                    ? flat.WebpageUrl
                    : $"https://www.youtube.com/watch?v={flat.Id}";

                try
                {
                    var full = _ytDlpClient.GetVideoInfo(videoUrl);
                    results.Add(MapToContentMetadata(full));
                }
                catch (Exception ex)
                {
                    _logger.Warn("Skipping video {0}: {1}", flat.Id, ex.Message);
                    results.Add(MapToContentMetadata(flat));
                }
            }

            return results;
        }

        // Use ytsearch1: to find a video from the named creator, extract its
        // channel_url, then fetch the full channel metadata from that URL.
        private YtDlpChannelInfo SearchAndResolveChannel(string query)
        {
            var searchUrl = $"ytsearch1:{query}";
            _logger.Debug("Searching yt-dlp: {0}", searchUrl);

            var searchResult = _ytDlpClient.GetChannelInfo(searchUrl);

            var firstEntry = searchResult.Entries.FirstOrDefault();
            if (firstEntry == null)
            {
                throw new InvalidOperationException($"No YouTube results found for: {query}");
            }

            var channelUrl = !string.IsNullOrWhiteSpace(firstEntry.ChannelUrl)
                ? firstEntry.ChannelUrl
                : firstEntry.UploaderUrl;

            if (string.IsNullOrWhiteSpace(channelUrl))
            {
                throw new InvalidOperationException(
                    $"Search returned a result but no channel URL could be extracted for: {query}");
            }

            _logger.Info("Name search resolved to channel: {0}", channelUrl);
            return _ytDlpClient.GetChannelInfo(channelUrl);
        }

        private static CreatorMetadataResult BuildResult(YtDlpChannelInfo channelInfo)
        {
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
                AirDateUtc = ParseUploadDate(video.UploadDate) ?? ParseTimestamp(video.Timestamp)
            };
        }

        private static DateTime? ParseTimestamp(double? timestamp)
        {
            if (!timestamp.HasValue || timestamp.Value <= 0)
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds((long)timestamp.Value).UtcDateTime;
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
    }
}
