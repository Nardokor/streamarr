using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
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
        private readonly IYouTubeApiClient _youTubeApiClient;
        private readonly Logger _logger;

        public YouTubeMetadataService(IYtDlpClient ytDlpClient, IYouTubeApiClient youTubeApiClient, Logger logger)
        {
            _ytDlpClient = ytDlpClient;
            _youTubeApiClient = youTubeApiClient;
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

        public List<ContentMetadataResult> GetNewContent(string platformUrl, string platformId, DateTime? since = null)
        {
            // Derive uploads playlist ID: "UC..." → "UU..."
            if (string.IsNullOrWhiteSpace(platformId) || !platformId.StartsWith("UC"))
            {
                throw new InvalidOperationException(
                    $"Cannot derive uploads playlist from channel ID '{platformId}'. Expected format: UCxxxxxxx");
            }

            var uploadsPlaylistId = string.Concat("UU", platformId.AsSpan(2));

            _logger.Info("Fetching playlist {0} via YouTube API (since: {1})", uploadsPlaylistId, since?.ToString("u") ?? "beginning");

            var playlistItems = _youTubeApiClient.GetPlaylistItems(uploadsPlaylistId, since);

            if (!playlistItems.Any())
            {
                _logger.Info("No new items found for playlist {0}", uploadsPlaylistId);
                return new List<ContentMetadataResult>();
            }

            _logger.Info("Found {0} new items, fetching details", playlistItems.Count);

            var videoDetails = _youTubeApiClient.GetVideoDetails(playlistItems.Select(p => p.VideoId));

            // Build a lookup so we can attach exact publish dates
            var publishedAtById = playlistItems.ToDictionary(p => p.VideoId, p => p.PublishedAt);

            return videoDetails
                .Select(v => MapToContentMetadata(v, publishedAtById.GetValueOrDefault(v.Id)))
                .ToList();
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

        private static ContentMetadataResult MapToContentMetadata(YoutubeVideo video, DateTime? publishedAt)
        {
            TimeSpan? duration = null;
            if (!string.IsNullOrWhiteSpace(video.ContentDetails?.Duration))
            {
                try
                {
                    duration = XmlConvert.ToTimeSpan(video.ContentDetails.Duration);
                }
                catch
                {
                    // malformed duration — leave null
                }
            }

            var thumbnailUrl = video.Snippet?.Thumbnails?.Medium?.Url
                ?? video.Snippet?.Thumbnails?.High?.Url;

            return new ContentMetadataResult
            {
                PlatformContentId = video.Id,
                ContentType = DetermineContentType(video, duration),
                Title = video.Snippet?.Title,
                Description = video.Snippet?.Description,
                ThumbnailUrl = thumbnailUrl,
                Duration = duration,
                AirDateUtc = publishedAt
            };
        }

        private static ContentType DetermineContentType(YoutubeVideo video, TimeSpan? duration)
        {
            if (video.LiveStreamingDetails != null)
            {
                return ContentType.Livestream;
            }

            if (duration.HasValue && duration.Value.TotalSeconds <= 60)
            {
                return ContentType.Short;
            }

            return ContentType.Video;
        }
    }
}
