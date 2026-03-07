#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using FluentValidation.Results;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Download.YtDlp;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.MetadataSource.YouTube
{
    public class YouTube : MetadataSourceBase<YouTubeSettings>
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

        public YouTube(IYtDlpClient ytDlpClient, IYouTubeApiClient youTubeApiClient, Logger logger)
        {
            _ytDlpClient = ytDlpClient;
            _youTubeApiClient = youTubeApiClient;
            _logger = logger;
        }

        public override string Name => "YouTube";
        public override PlatformType Platform => PlatformType.YouTube;

        public override IEnumerable<ProviderDefinition> DefaultDefinitions =>
            new List<ProviderDefinition>
            {
                new MetadataSourceDefinition
                {
                    Name = "YouTube",
                    Implementation = nameof(YouTube),
                    ConfigContract = nameof(YouTubeSettings),
                    Platform = PlatformType.YouTube,
                    Enable = true,
                    Settings = new YouTubeSettings()
                }
            };

        public override ValidationResult Test()
        {
            var apiKey = Settings.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new ValidationResult();
            }

            try
            {
                _youTubeApiClient.TestApiKey(apiKey);
            }
            catch (Exception ex)
            {
                return new ValidationResult(new[]
                {
                    new ValidationFailure("ApiKey", $"API key test failed: {ex.Message}")
                });
            }

            return new ValidationResult();
        }

        // ── Discovery ──────────────────────────────────────────────────────────

        public override CreatorMetadataResult SearchCreator(string query)
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

            var result = BuildResult(channelInfo);

            // Supplement yt-dlp thumbnail with YouTube API result (more reliable URL)
            var channelId = result.Channels.FirstOrDefault()?.PlatformId;
            if (!string.IsNullOrWhiteSpace(channelId))
            {
                var apiThumbnail = _youTubeApiClient.GetChannelThumbnailUrl(Settings.ApiKey, channelId);
                if (!string.IsNullOrEmpty(apiThumbnail))
                {
                    result.ThumbnailUrl = apiThumbnail;
                    if (result.Channels.Count > 0)
                    {
                        result.Channels[0].ThumbnailUrl = apiThumbnail;
                    }
                }
            }

            return result;
        }

        public override ChannelMetadataResult GetChannelMetadata(string platformUrl)
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

            var thumbnailUrl = channelInfo.Thumbnail;
            if (!string.IsNullOrWhiteSpace(channelId))
            {
                var apiThumbnail = _youTubeApiClient.GetChannelThumbnailUrl(Settings.ApiKey, channelId);
                if (!string.IsNullOrEmpty(apiThumbnail))
                {
                    thumbnailUrl = apiThumbnail;
                }
            }

            return new ChannelMetadataResult
            {
                Platform = PlatformType.YouTube,
                PlatformId = channelId,
                PlatformUrl = channelPageUrl,
                Title = channelName,
                Description = channelInfo.Description,
                ThumbnailUrl = thumbnailUrl
            };
        }

        // ── Content sync ───────────────────────────────────────────────────────

        public override IEnumerable<ContentMetadataResult> GetNewContent(string platformUrl, string platformId, DateTime? since)
        {
            if (_ytDlpClient.HasCookies)
            {
                _logger.Info("Cookie file configured — using yt-dlp listing to include members-only content");
                return GetNewContentHybrid(platformUrl, since);
            }

            return GetNewContentViaApi(platformId, since);
        }

        private IEnumerable<ContentMetadataResult> GetNewContentViaApi(string platformId, DateTime? since)
        {
            // Derive uploads playlist ID: "UC..." → "UU..."
            if (string.IsNullOrWhiteSpace(platformId) || !platformId.StartsWith("UC"))
            {
                throw new InvalidOperationException(
                    $"Cannot derive uploads playlist from channel ID '{platformId}'. Expected format: UCxxxxxxx");
            }

            var uploadsPlaylistId = string.Concat("UU", platformId.AsSpan(2));

            _logger.Info("Fetching playlist {0} via YouTube API (since: {1})", uploadsPlaylistId, since?.ToString("u") ?? "beginning");

            var playlistItems = _youTubeApiClient.GetPlaylistItems(Settings.ApiKey, uploadsPlaylistId, since);

            if (!playlistItems.Any())
            {
                _logger.Info("No new items found for playlist {0}", uploadsPlaylistId);
                return Enumerable.Empty<ContentMetadataResult>();
            }

            _logger.Info("Found {0} new items, fetching details", playlistItems.Count);

            var videoDetails = _youTubeApiClient.GetVideoDetails(Settings.ApiKey, playlistItems.Select(p => p.VideoId));

            var publishedAtById = playlistItems.ToDictionary(p => p.VideoId, p => p.PublishedAt);

            return videoDetails.Select(v => MapToContentMetadata(v, publishedAtById.GetValueOrDefault(v.Id)));
        }

        private IEnumerable<ContentMetadataResult> GetNewContentHybrid(string platformUrl, DateTime? since)
        {
            // 1. Regular tabs: videos, shorts, streams (date-filtered for efficiency)
            var dateAfter = since?.ToString("yyyyMMdd");
            var regularVideos = _ytDlpClient.GetChannelVideos(platformUrl, limit: null, dateAfter: dateAfter);

            // 2. Membership tab: always fetch all so historical members content is backfilled
            var membershipVideos = _ytDlpClient.GetMembershipTabVideos(platformUrl);
            var membershipIds = new HashSet<string>(
                membershipVideos.Select(v => v.Id).Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);

            if (membershipIds.Count > 0)
            {
                _logger.Info("{0} video(s) found in membership tab", membershipIds.Count);
            }

            // 3. Merge: regular + membership-exclusive videos (union by ID)
            var seen = new HashSet<string>(
                regularVideos.Select(v => v.Id).Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
            var allVideos = regularVideos.ToList();
            foreach (var v in membershipVideos)
            {
                if (!string.IsNullOrWhiteSpace(v.Id) && seen.Add(v.Id))
                {
                    allVideos.Add(v);
                }
            }

            if (!allVideos.Any())
            {
                _logger.Info("yt-dlp found no new content at {0}", platformUrl);
                return Enumerable.Empty<ContentMetadataResult>();
            }

            _logger.Info("yt-dlp found {0} items ({1} from membership tab) for {2}", allVideos.Count, membershipIds.Count, platformUrl);

            // 4. YouTube API enrichment for richer metadata (timestamps, liveStreamingDetails)
            var apiById = new Dictionary<string, YoutubeVideo>();
            if (!string.IsNullOrWhiteSpace(Settings.ApiKey))
            {
                var apiVideos = _youTubeApiClient.GetVideoDetails(Settings.ApiKey, allVideos.Select(v => v.Id));
                foreach (var v in apiVideos)
                {
                    apiById[v.Id] = v;
                }
            }

            // 5. Map: IsMembers is determined solely by membership tab presence
            return allVideos.Select(v =>
            {
                var isMembers = membershipIds.Contains(v.Id);
                return apiById.TryGetValue(v.Id, out var apiVideo)
                    ? MapToContentMetadata(apiVideo, publishedAt: null, isMembers: isMembers)
                    : MapYtDlpToContentMetadata(v, isMembers: isMembers);
            });
        }

        // ── Single / batch lookup ──────────────────────────────────────────────

        public override ContentMetadataResult? GetContentMetadata(string platformContentId)
        {
            var results = GetVideoDetails(new[] { platformContentId });
            return results.FirstOrDefault();
        }

        public override IEnumerable<ContentMetadataResult> GetContentMetadataBatch(IEnumerable<string> platformContentIds)
        {
            return GetVideoDetails(platformContentIds);
        }

        private IEnumerable<ContentMetadataResult> GetVideoDetails(IEnumerable<string> platformContentIds)
        {
            var ids = platformContentIds.ToList();
            if (!ids.Any())
            {
                return Enumerable.Empty<ContentMetadataResult>();
            }

            if (string.IsNullOrWhiteSpace(Settings.ApiKey))
            {
                return Enumerable.Empty<ContentMetadataResult>();
            }

            var videos = _youTubeApiClient.GetVideoDetails(Settings.ApiKey, ids);
            return videos.Select(v => MapToContentMetadata(v, null));
        }

        // ── Livestream status ──────────────────────────────────────────────────

        public override IEnumerable<ContentStatusUpdate> GetLivestreamStatusUpdates(IEnumerable<string> platformContentIds)
        {
            var ids = platformContentIds.ToList();
            if (!ids.Any() || string.IsNullOrWhiteSpace(Settings.ApiKey))
            {
                return Enumerable.Empty<ContentStatusUpdate>();
            }

            var videos = _youTubeApiClient.GetVideoDetails(Settings.ApiKey, ids);

            return videos
                .Select(MapToStatusUpdate)
                .OfType<ContentStatusUpdate>();
        }

        private static ContentStatusUpdate? MapToStatusUpdate(YoutubeVideo video)
        {
            var lsd = video.LiveStreamingDetails;
            if (lsd == null)
            {
                return null;
            }

            if (lsd.ScheduledStartTime.HasValue && lsd.ScheduledStartTime.Value > DateTime.UtcNow)
            {
                return new ContentStatusUpdate
                {
                    PlatformContentId = video.Id,
                    NewContentType = ContentType.Upcoming,
                    NewAirDateUtc = lsd.ScheduledStartTime.Value,
                    ExistsOnPlatform = true,
                    ShouldTriggerDownload = false
                };
            }

            if (lsd.ActualStartTime.HasValue && !lsd.ActualEndTime.HasValue)
            {
                return new ContentStatusUpdate
                {
                    PlatformContentId = video.Id,
                    NewContentType = ContentType.Live,
                    NewAirDateUtc = lsd.ActualStartTime.Value,
                    ExistsOnPlatform = true,
                    ShouldTriggerDownload = true
                };
            }

            if (lsd.ActualStartTime.HasValue)
            {
                return new ContentStatusUpdate
                {
                    PlatformContentId = video.Id,
                    NewContentType = ContentType.Vod,
                    NewAirDateUtc = lsd.ActualStartTime.Value,
                    ExistsOnPlatform = true,
                    ShouldTriggerDownload = false
                };
            }

            return null;
        }

        // ── Accessibility probe ────────────────────────────────────────────────

        public override bool ProbeContentAccessibility(string platformContentId)
        {
            try
            {
                var url = $"https://www.youtube.com/watch?v={platformContentId}";
                _ytDlpClient.GetVideoInfo(url);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Members video {0} is not accessible with current cookies", platformContentId);
                return false;
            }
        }

        // ── Mapping ────────────────────────────────────────────────────────────

        private static ContentMetadataResult MapYtDlpToContentMetadata(YtDlpVideoInfo video, bool isMembers = false)
        {
            // Prefer unix timestamp (second precision) over upload_date (day precision)
            DateTime? airDate = null;
            if (video.Timestamp.HasValue)
            {
                airDate = DateTimeOffset.FromUnixTimeSeconds((long)video.Timestamp.Value).UtcDateTime;
            }
            else if (video.UploadDate?.Length == 8 &&
                     DateTime.TryParseExact(
                         video.UploadDate,
                         "yyyyMMdd",
                         CultureInfo.InvariantCulture,
                         DateTimeStyles.AssumeUniversal,
                         out var parsed))
            {
                airDate = parsed.ToUniversalTime();
            }

            var contentType = ContentType.Video;
            if (video.IsLive == true)
            {
                contentType = ContentType.Live;
            }
            else if (video.WasLive == true)
            {
                contentType = ContentType.Vod;
            }
            else if (video.Duration is <= 60)
            {
                contentType = ContentType.Short;
            }

            return new ContentMetadataResult
            {
                PlatformContentId = video.Id,
                PlatformChannelId = video.ChannelId,
                PlatformChannelTitle = video.Channel,
                ContentType = contentType,
                Title = video.Title,
                Description = video.Description,
                ThumbnailUrl = YouTubeApiClient.NormalizeThumbnailUrl(video.Thumbnail),
                Duration = video.Duration.HasValue ? TimeSpan.FromSeconds(video.Duration.Value) : null,
                AirDateUtc = airDate,
                IsMembers = isMembers,
                IsAccessible = true,
            };
        }

        // ── Helpers ────────────────────────────────────────────────────────────

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

            var thumbnailUrl = YouTubeApiClient.NormalizeThumbnailUrl(channelInfo.Thumbnail);

            return new CreatorMetadataResult
            {
                Name = channelName,
                Description = channelInfo.Description,
                ThumbnailUrl = thumbnailUrl,
                Channels = new List<ChannelMetadataResult>
                {
                    new ChannelMetadataResult
                    {
                        Platform = PlatformType.YouTube,
                        PlatformId = channelId,
                        PlatformUrl = channelPageUrl,
                        Title = channelName,
                        Description = channelInfo.Description,
                        ThumbnailUrl = thumbnailUrl
                    }
                }
            };
        }

        private static ContentMetadataResult MapToContentMetadata(YoutubeVideo video, DateTime? publishedAt, bool isMembers = false)
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

            var thumbnailUrl = YouTubeApiClient.NormalizeThumbnailUrl(
                video.Snippet?.Thumbnails?.Medium?.Url
                ?? video.Snippet?.Thumbnails?.High?.Url
                ?? string.Empty);

            return new ContentMetadataResult
            {
                PlatformContentId = video.Id,
                PlatformChannelId = video.Snippet?.ChannelId ?? string.Empty,
                PlatformChannelTitle = video.Snippet?.ChannelTitle ?? string.Empty,
                ContentType = DetermineContentType(video, duration),
                Title = video.Snippet?.Title ?? string.Empty,
                Description = video.Snippet?.Description ?? string.Empty,
                ThumbnailUrl = thumbnailUrl,
                Duration = duration,
                AirDateUtc = DetermineAirDate(video, publishedAt),
                IsMembers = isMembers,
                IsAccessible = true,
            };
        }

        private static DateTime? DetermineAirDate(YoutubeVideo video, DateTime? publishedAt)
        {
            var lsd = video.LiveStreamingDetails;
            if (lsd != null)
            {
                if (lsd.ScheduledStartTime.HasValue && lsd.ScheduledStartTime.Value > DateTime.UtcNow)
                {
                    return lsd.ScheduledStartTime.Value;
                }

                if (lsd.ActualStartTime.HasValue)
                {
                    return lsd.ActualStartTime.Value;
                }
            }

            return publishedAt;
        }

        private static ContentType DetermineContentType(YoutubeVideo video, TimeSpan? duration)
        {
            var lsd = video.LiveStreamingDetails;
            if (lsd != null)
            {
                if (lsd.ActualStartTime.HasValue && !lsd.ActualEndTime.HasValue)
                {
                    return ContentType.Live;
                }

                if (lsd.ScheduledStartTime.HasValue && lsd.ScheduledStartTime.Value > DateTime.UtcNow)
                {
                    return ContentType.Upcoming;
                }

                return ContentType.Vod;
            }

            if (duration.HasValue && duration.Value.TotalSeconds <= 60)
            {
                return ContentType.Short;
            }

            return ContentType.Video;
        }
    }
}
