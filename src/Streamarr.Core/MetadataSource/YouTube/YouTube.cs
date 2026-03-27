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

            // Use yt-dlp thumbnail directly — avoids a channels.list API call (1 unit)
            // on every sync cycle. The API thumbnail fetch is reserved for SearchCreator
            // (one-time cost on add) where higher-quality is worth the unit.
            return new ChannelMetadataResult
            {
                Platform = PlatformType.YouTube,
                PlatformId = channelId,
                PlatformUrl = channelPageUrl,
                Title = channelName,
                Description = channelInfo.Description,
                ThumbnailUrl = YouTubeApiClient.NormalizeThumbnailUrl(channelInfo.BestAvatarUrl)
            };
        }

        // ── Content sync ───────────────────────────────────────────────────────

        public override IEnumerable<ContentMetadataResult> GetNewContent(string platformUrl, string platformId, DateTime? since, bool checkMembership = false)
        {
            if (!string.IsNullOrWhiteSpace(Settings.ApiKey))
            {
                if (since == null)
                {
                    _logger.Info("Initial sync — fetching full playlist via API for {0}", platformUrl);
                    return GetInitialSyncViaApi(platformId, platformUrl, checkMembership);
                }

                _logger.Info("Incremental sync — using RSS for {0}", platformUrl);
                return GetIncrementalSyncViaRss(platformId, platformUrl, since.Value, checkMembership);
            }

            // No API key: fall back to yt-dlp for public content.
            // Membership content requires cookies; without either, only public yt-dlp metadata is returned.
            _logger.Info("No API key — using yt-dlp listing (checkMembership={0})", checkMembership);
            return GetNewContentHybrid(platformUrl, platformId, since, checkMembership);
        }

        // Initial sync: fetch the full uploads playlist via API so we have all historical content.
        // RSS is added as a supplement to catch any active live stream not yet in the playlist.
        // Cookies are used only for the membership tab when checkMembership is true.
        private IEnumerable<ContentMetadataResult> GetInitialSyncViaApi(
            string platformId, string platformUrl, bool checkMembership)
        {
            var uploadsPlaylistId = DeriveUploadsPlaylistId(platformId);

            _logger.Info("Fetching full playlist {0} (initial sync)", uploadsPlaylistId);
            var playlistItems = _youTubeApiClient.GetPlaylistItems(Settings.ApiKey, uploadsPlaylistId, since: null);
            var playlistIds = new HashSet<string>(playlistItems.Select(p => p.VideoId), StringComparer.OrdinalIgnoreCase);

            var rssExtraIds = FetchRssExtras(platformId, platformUrl, playlistIds);

            var allPublicIds = playlistItems.Select(p => p.VideoId).Concat(rssExtraIds);
            var publishedAtById = playlistItems.ToDictionary(p => p.VideoId, p => p.PublishedAt);
            var videoDetails = _youTubeApiClient.GetVideoDetails(Settings.ApiKey, allPublicIds);
            var result = videoDetails
                .Select(v => MapToContentMetadata(v, publishedAtById.GetValueOrDefault(v.Id)))
                .ToList();

            _logger.Info("Initial sync: {0} public item(s) for {1}", result.Count, platformUrl);

            if (checkMembership && _ytDlpClient.HasCookies)
            {
                var seen = new HashSet<string>(result.Select(r => r.PlatformContentId), StringComparer.OrdinalIgnoreCase);
                result.AddRange(FetchMembershipContent(platformUrl, seen));
            }

            return result;
        }

        // Incremental sync: RSS gives us the 15 most recent video IDs (free, no quota).
        // One GetVideoDetails call confirms types and catches live streams.
        // Falls back to the playlist API if RSS is unavailable.
        private IEnumerable<ContentMetadataResult> GetIncrementalSyncViaRss(
            string platformId, string platformUrl, DateTime since, bool checkMembership)
        {
            List<string> rssIds;
            try
            {
                rssIds = _youTubeApiClient.GetChannelRecentVideoIds(platformId);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "RSS fetch failed for '{0}' — falling back to playlist API", platformUrl);
                return GetIncrementalSyncViaApi(platformId, platformUrl, since, checkMembership);
            }

            if (!rssIds.Any())
            {
                _logger.Warn("RSS returned no IDs for '{0}' — falling back to playlist API", platformUrl);
                return GetIncrementalSyncViaApi(platformId, platformUrl, since, checkMembership);
            }

            _logger.Info("RSS: {0} recent ID(s) for {1}", rssIds.Count, platformUrl);

            var videoDetails = _youTubeApiClient.GetVideoDetails(Settings.ApiKey, rssIds);
            var result = videoDetails.Select(v => MapToContentMetadata(v, publishedAt: null)).ToList();

            if (checkMembership && _ytDlpClient.HasCookies)
            {
                var seen = new HashSet<string>(result.Select(r => r.PlatformContentId), StringComparer.OrdinalIgnoreCase);
                result.AddRange(FetchMembershipContent(platformUrl, seen));
            }

            return result;
        }

        // RSS fallback: used when the RSS feed is unavailable (outage, rate-limit, etc.).
        // Fetches only content newer than `since` from the uploads playlist.
        private IEnumerable<ContentMetadataResult> GetIncrementalSyncViaApi(
            string platformId, string platformUrl, DateTime since, bool checkMembership)
        {
            var uploadsPlaylistId = DeriveUploadsPlaylistId(platformId);
            _logger.Info("Fetching playlist {0} since {1} (RSS fallback)", uploadsPlaylistId, since.ToString("u"));

            var playlistItems = _youTubeApiClient.GetPlaylistItems(Settings.ApiKey, uploadsPlaylistId, since);
            var result = new List<ContentMetadataResult>();

            if (playlistItems.Any())
            {
                var publishedAtById = playlistItems.ToDictionary(p => p.VideoId, p => p.PublishedAt);
                var videoDetails = _youTubeApiClient.GetVideoDetails(Settings.ApiKey, playlistItems.Select(p => p.VideoId));
                result.AddRange(videoDetails.Select(v => MapToContentMetadata(v, publishedAtById.GetValueOrDefault(v.Id))));
            }

            if (checkMembership && _ytDlpClient.HasCookies)
            {
                var seen = new HashSet<string>(result.Select(r => r.PlatformContentId), StringComparer.OrdinalIgnoreCase);
                result.AddRange(FetchMembershipContent(platformUrl, seen));
            }

            return result;
        }

        // Fetches RSS IDs not already in the known set (used by initial sync as a live-stream supplement).
        private List<string> FetchRssExtras(string platformId, string platformUrl, HashSet<string> alreadyKnown)
        {
            try
            {
                var rssIds = _youTubeApiClient.GetChannelRecentVideoIds(platformId);
                var extras = rssIds.Where(id => !alreadyKnown.Contains(id)).ToList();
                _logger.Info(
                    "RSS supplement: {0} recent ID(s) fetched, {1} not yet in playlist for {2}",
                    rssIds.Count,
                    extras.Count,
                    platformUrl);
                return extras;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "RSS supplement failed for '{0}'; active live stream may not be detected until next sync", platformUrl);
                return new List<string>();
            }
        }

        // Fetches membership-tab content via yt-dlp (requires cookies) and enriches via API if available.
        // Only IDs not already in alreadySeen are returned to avoid duplicates.
        private List<ContentMetadataResult> FetchMembershipContent(string platformUrl, HashSet<string> alreadySeen)
        {
            var membershipVideos = _ytDlpClient.GetMembershipTabVideos(platformUrl);
            var membershipIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newVideos = new List<YtDlpVideoInfo>();
            foreach (var v in membershipVideos)
            {
                if (string.IsNullOrWhiteSpace(v.Id))
                {
                    continue;
                }

                membershipIds.Add(v.Id);
                if (!alreadySeen.Contains(v.Id))
                {
                    newVideos.Add(v);
                }
            }

            if (membershipIds.Count > 0)
            {
                _logger.Info("{0} membership video(s) found ({1} new) for {2}", membershipIds.Count, newVideos.Count, platformUrl);
            }

            if (!newVideos.Any())
            {
                return new List<ContentMetadataResult>();
            }

            var apiById = new Dictionary<string, YoutubeVideo>();
            if (!string.IsNullOrWhiteSpace(Settings.ApiKey))
            {
                var apiVideos = _youTubeApiClient.GetVideoDetails(Settings.ApiKey, newVideos.Select(v => v.Id));
                foreach (var v in apiVideos)
                {
                    apiById[v.Id] = v;
                }
            }

            return newVideos
                .Select(v => apiById.TryGetValue(v.Id, out var apiVideo)
                    ? MapToContentMetadata(apiVideo, publishedAt: null, isMembers: true)
                    : MapYtDlpToContentMetadata(v, isMembers: true))
                .ToList();
        }

        // No-API-key fallback: full yt-dlp listing with optional membership tab.
        // Used only when Settings.ApiKey is not configured.
        // Rich metadata (liveStreamingDetails, precise timestamps) is unavailable without an API key.
        private IEnumerable<ContentMetadataResult> GetNewContentHybrid(string platformUrl, string platformId, DateTime? since, bool checkMembership)
        {
            var dateAfter = since?.ToString("yyyyMMdd");
            var regularVideos = _ytDlpClient.GetChannelVideos(platformUrl, limit: null, dateAfter: dateAfter);

            var membershipIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var membershipVideos = new List<YtDlpVideoInfo>();
            if (checkMembership && _ytDlpClient.HasCookies)
            {
                membershipVideos = _ytDlpClient.GetMembershipTabVideos(platformUrl);
                foreach (var v in membershipVideos)
                {
                    if (!string.IsNullOrWhiteSpace(v.Id))
                    {
                        membershipIds.Add(v.Id);
                    }
                }

                if (membershipIds.Count > 0)
                {
                    _logger.Info("{0} video(s) found in membership tab", membershipIds.Count);
                }
            }

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

            var membersOnlyCount = 0;
            var result = allVideos.Select(v =>
            {
                var isMembers = membershipIds.Contains(v.Id)
                    || string.Equals(v.Availability, "subscriber_only", StringComparison.OrdinalIgnoreCase);
                if (isMembers)
                {
                    membersOnlyCount++;
                }

                return MapYtDlpToContentMetadata(v, isMembers: isMembers);
            }).ToList();

            if (membersOnlyCount > 0)
            {
                _logger.Info("{0} members-only video(s) identified", membersOnlyCount);
            }

            return result;
        }

        private static string DeriveUploadsPlaylistId(string platformId)
        {
            if (string.IsNullOrWhiteSpace(platformId) || !platformId.StartsWith("UC"))
            {
                throw new InvalidOperationException(
                    $"Cannot derive uploads playlist from channel ID '{platformId}'. Expected format: UCxxxxxxx");
            }

            return string.Concat("UU", platformId.AsSpan(2));
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

            var broadcastContent = video.Snippet?.LiveBroadcastContent ?? string.Empty;

            // Check actualStartTime + no actualEndTime first — this is authoritative.
            // broadcastContent can lag several minutes behind the real stream state,
            // so a stream that is already live may still say "upcoming" in the API.
            if (lsd.ActualStartTime.HasValue && !lsd.ActualEndTime.HasValue)
            {
                return new ContentStatusUpdate
                {
                    PlatformContentId = video.Id,
                    NewContentType = ContentType.Live,
                    NewAirDateUtc = lsd.ActualStartTime,
                    ExistsOnPlatform = true,
                    ShouldTriggerDownload = true
                };
            }

            // Stream has ended (actualEndTime is set).
            if (lsd.ActualStartTime.HasValue && lsd.ActualEndTime.HasValue)
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

            // No actualStartTime yet — stream hasn't started.
            if (broadcastContent == "upcoming" ||
                (lsd.ScheduledStartTime.HasValue && lsd.ScheduledStartTime.Value > DateTime.UtcNow))
            {
                return new ContentStatusUpdate
                {
                    PlatformContentId = video.Id,
                    NewContentType = ContentType.Upcoming,
                    NewAirDateUtc = lsd.ScheduledStartTime,
                    ExistsOnPlatform = true,
                    ShouldTriggerDownload = false
                };
            }

            return null;
        }

        // ── Accessibility probe ────────────────────────────────────────────────

        public override bool ProbeContentAccessibility(string platformContentId)
        {
            // Uses --print availability (metadata-only fetch) to avoid triggering
            // deno/JS-challenge format resolution — inaccessible videos fail before
            // format extraction with a clear membership error.
            return _ytDlpClient.IsVideoAccessible(platformContentId);
        }

        public override ContentMetadataResult? GetActiveLivestream(string platformUrl, string platformId)
        {
            // Use CHANNEL_URL/live — YouTube routes this directly to the active live stream
            // if one exists, or returns an error if the channel is offline.  This avoids the
            // GetChannelInfo two-level tab/playlist structure where entries[] contains tab
            // playlists (Videos, Live, Shorts), not individual videos, so IsLive is never set.
            var baseUrl = platformUrl.TrimEnd('/');
            foreach (var knownTab in new[] { "/videos", "/shorts", "/streams", "/live" })
            {
                if (baseUrl.EndsWith(knownTab, StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = baseUrl[..^knownTab.Length];
                    break;
                }
            }

            var liveUrl = baseUrl + "/live";

            try
            {
                var video = _ytDlpClient.GetVideoInfo(liveUrl);

                if (video.IsLive != true)
                {
                    return null;
                }

                return new ContentMetadataResult
                {
                    PlatformContentId = video.Id,
                    ContentType = ContentType.Live,
                    Title = string.IsNullOrEmpty(video.Title) ? "Live Stream" : video.Title,
                    ThumbnailUrl = video.Thumbnail,
                    AirDateUtc = DateTime.UtcNow,
                };
            }
            catch (Exception ex) when (ex.Message.Contains("not currently live", StringComparison.OrdinalIgnoreCase))
            {
                // Channel is offline — this is the normal case, not an error.
                _logger.Debug("Channel '{0}' is not currently live", liveUrl);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to check active livestream for '{0}'", liveUrl);
                return null;
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

            var thumbnailUrl = YouTubeApiClient.NormalizeThumbnailUrl(channelInfo.BestAvatarUrl);

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

            if (publishedAt.HasValue)
            {
                return publishedAt.Value;
            }

            // Hybrid path passes publishedAt=null — fall back to snippet date from the API
            if (!string.IsNullOrWhiteSpace(video.Snippet?.PublishedAt) &&
                DateTime.TryParse(video.Snippet.PublishedAt, null, DateTimeStyles.RoundtripKind, out var snippetDate))
            {
                return snippetDate.ToUniversalTime();
            }

            return null;
        }

        private static ContentType DetermineContentType(YoutubeVideo video, TimeSpan? duration)
        {
            var lsd = video.LiveStreamingDetails;
            if (lsd != null)
            {
                // snippet.liveBroadcastContent is authoritative: "live", "upcoming", or "none".
                // YouTube sometimes omits ActualEndTime for completed streams, so relying solely
                // on ActualStartTime.HasValue && !ActualEndTime.HasValue produces false positives.
                var broadcastContent = video.Snippet?.LiveBroadcastContent ?? string.Empty;

                if (broadcastContent == "live" ||
                    (lsd.ActualStartTime.HasValue && !lsd.ActualEndTime.HasValue && broadcastContent != "none"))
                {
                    return ContentType.Live;
                }

                if (broadcastContent == "upcoming" ||
                    (lsd.ScheduledStartTime.HasValue && lsd.ScheduledStartTime.Value > DateTime.UtcNow && broadcastContent != "none"))
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
