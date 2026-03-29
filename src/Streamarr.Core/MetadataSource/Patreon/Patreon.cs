#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentValidation.Results;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;

namespace Streamarr.Core.MetadataSource.Patreon
{
    public class Patreon : MetadataSourceBase<PatreonSettings>
    {
        // Post types that contain downloadable video content (yt-dlp Patreon extractor).
        private static readonly HashSet<string> VideoPostTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "video_file",
            "video_external_file",
            "video_embed",        // unlisted YouTube video embedded in the post
            "livestream_youtube", // YouTube livestream VOD linked from the post
        };

        private readonly IPatreonApiClient _client;
        private readonly Logger _logger;

        public Patreon(IPatreonApiClient client, Logger logger)
        {
            _client = client;
            _logger = logger;
        }

        public override string Name => "Patreon";
        public override PlatformType Platform => PlatformType.Patreon;

        // ── Validation ─────────────────────────────────────────────────────────

        public override ValidationResult Test()
        {
            if (string.IsNullOrWhiteSpace(Settings.CookiesFilePath))
            {
                return new ValidationResult(new[]
                {
                    new ValidationFailure("CookiesFilePath", "Cookies file path is required.")
                });
            }

            if (!File.Exists(Settings.CookiesFilePath))
            {
                return new ValidationResult(new[]
                {
                    new ValidationFailure("CookiesFilePath", $"Cookies file not found: {Settings.CookiesFilePath}")
                });
            }

            return new ValidationResult();
        }

        // ── Discovery ──────────────────────────────────────────────────────────

        public override CreatorMetadataResult SearchCreator(string query)
        {
            query = (query ?? string.Empty).Trim();

            var vanity = ExtractVanity(query);
            if (string.IsNullOrWhiteSpace(vanity))
            {
                throw new InvalidOperationException(
                    "Please enter a Patreon URL (e.g. https://www.patreon.com/creatorname) or username.");
            }

            _logger.Info("Looking up Patreon campaign for vanity: {0}", vanity);

            var campaign = _client.GetCampaignByVanity(Settings.CookiesFilePath, vanity);
            if (campaign == null)
            {
                throw new InvalidOperationException(
                    $"No Patreon campaign found for '{vanity}'. " +
                    "Check the URL or username, and ensure your cookies are valid.");
            }

            return BuildCreatorResult(campaign);
        }

        public override ChannelMetadataResult GetChannelMetadata(string platformUrl)
        {
            var vanity = ExtractVanity(platformUrl);
            if (string.IsNullOrWhiteSpace(vanity))
            {
                throw new InvalidOperationException($"Cannot derive Patreon vanity from URL: {platformUrl}");
            }

            var campaign = _client.GetCampaignByVanity(Settings.CookiesFilePath, vanity);
            if (campaign == null)
            {
                throw new InvalidOperationException(
                    $"Patreon campaign for '{vanity}' not found. Check your cookies file.");
            }

            return BuildChannelResult(campaign);
        }

        // ── Content sync ───────────────────────────────────────────────────────

        public override IEnumerable<ContentMetadataResult> GetNewContent(
            string platformUrl,
            string platformId,
            DateTime? since,
            bool checkMembership = false)
        {
            _logger.Info("Fetching Patreon posts for campaign {0} (since: {1})", platformId, since?.ToString("u") ?? "all");

            var posts = _client.GetCampaignPosts(Settings.CookiesFilePath, platformId, since);
            _logger.Info("Patreon: fetched {0} total posts for campaign {1}", posts.Count, platformId);

            var results = new List<ContentMetadataResult>();

            foreach (var post in posts)
            {
                var attrs = post.Attributes;
                if (attrs == null)
                {
                    continue;
                }

                if (!IsVideoPost(attrs.PostType))
                {
                    _logger.Debug("Skipping post {0} (type='{1}', title='{2}')", post.Id, attrs.PostType, attrs.Title);
                    continue;
                }

                var airDate = ParsePublishedAt(attrs.PublishedAt);
                var contentType = string.Equals(attrs.PostType, "livestream_youtube", StringComparison.OrdinalIgnoreCase)
                    ? ContentType.Vod
                    : ContentType.Video;

                results.Add(new ContentMetadataResult
                {
                    PlatformContentId = post.Id,
                    PlatformChannelId = platformId,
                    PlatformChannelTitle = attrs.Title ?? string.Empty,
                    ContentType = contentType,
                    Title = attrs.Title ?? $"Patreon post {post.Id}",
                    Description = attrs.Content ?? string.Empty,
                    ThumbnailUrl = attrs.ThumbnailUrl ?? string.Empty,
                    AirDateUtc = airDate,
                    IsAccessible = true
                });
            }

            _logger.Info("GetNewContent for Patreon campaign {0}: {1} video post(s)", platformId, results.Count);
            return results;
        }

        // ── Single / batch lookup ──────────────────────────────────────────────

        public override ContentMetadataResult? GetContentMetadata(string platformContentId)
        {
            try
            {
                var post = _client.GetPost(Settings.CookiesFilePath, platformContentId);
                if (post?.Attributes == null || !IsVideoPost(post.Attributes.PostType))
                {
                    return null;
                }

                return new ContentMetadataResult
                {
                    PlatformContentId = post.Id,
                    PlatformChannelId = string.Empty,
                    PlatformChannelTitle = string.Empty,
                    ContentType = ContentType.Video,
                    Title = post.Attributes.Title ?? $"Patreon post {post.Id}",
                    Description = post.Attributes.Content ?? string.Empty,
                    ThumbnailUrl = post.Attributes.ThumbnailUrl ?? string.Empty,
                    AirDateUtc = ParsePublishedAt(post.Attributes.PublishedAt),
                    IsAccessible = true
                };
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to fetch Patreon post {0}", platformContentId);
                return null;
            }
        }

        public override IEnumerable<ContentMetadataResult> GetContentMetadataBatch(
            IEnumerable<string> platformContentIds)
        {
            var results = new List<ContentMetadataResult>();
            foreach (var id in platformContentIds)
            {
                var result = GetContentMetadata(id);
                if (result != null)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        // ── Livestream status ──────────────────────────────────────────────────

        public override IEnumerable<ContentStatusUpdate> GetLivestreamStatusUpdates(
            IEnumerable<string> platformContentIds)
        {
            // Patreon does not support live streams.
            return Enumerable.Empty<ContentStatusUpdate>();
        }

        public override string GetDownloadUrl(string platformContentId) =>
            $"https://www.patreon.com/posts/{platformContentId}";

        // ── Helpers ────────────────────────────────────────────────────────────

        private static CreatorMetadataResult BuildCreatorResult(PatreonCampaignResource campaign)
        {
            var channel = BuildChannelResult(campaign);
            return new CreatorMetadataResult
            {
                Name = campaign.Attributes?.CreationName
                       ?? campaign.Attributes?.Name
                       ?? campaign.Attributes?.Vanity
                       ?? campaign.Id,
                Description = campaign.Attributes?.Summary ?? string.Empty,
                ThumbnailUrl = campaign.Attributes?.ImageUrl ?? string.Empty,
                Channels = new List<ChannelMetadataResult> { channel }
            };
        }

        private static ChannelMetadataResult BuildChannelResult(PatreonCampaignResource campaign)
        {
            var url = campaign.Attributes?.Url
                      ?? (campaign.Attributes?.Vanity != null
                          ? $"https://www.patreon.com/{campaign.Attributes.Vanity}"
                          : "https://www.patreon.com/");

            return new ChannelMetadataResult
            {
                Platform = PlatformType.Patreon,
                PlatformId = campaign.Id,
                PlatformUrl = url,
                Title = campaign.Attributes?.CreationName
                        ?? campaign.Attributes?.Name
                        ?? campaign.Attributes?.Vanity
                        ?? campaign.Id,
                Description = campaign.Attributes?.Summary ?? string.Empty,
                ThumbnailUrl = campaign.Attributes?.ImageUrl ?? string.Empty
            };
        }

        private static bool IsVideoPost(string? postType)
        {
            return !string.IsNullOrWhiteSpace(postType) && VideoPostTypes.Contains(postType);
        }

        private static DateTime? ParsePublishedAt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var dt))
            {
                return dt.ToUniversalTime();
            }

            return null;
        }

        // Extracts the vanity slug from a Patreon URL or returns the bare slug as-is.
        private static string? ExtractVanity(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(input);
                    if (uri.Host.IndexOf("patreon.com", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        return null;
                    }

                    var segments = uri.AbsolutePath.Trim('/').Split('/');
                    if (segments.Length == 0 || string.IsNullOrWhiteSpace(segments[0]))
                    {
                        return null;
                    }

                    // Skip "c" prefix used by newer Patreon URLs (/c/creatorname)
                    return segments[0].Equals("c", StringComparison.OrdinalIgnoreCase) && segments.Length > 1
                        ? segments[1]
                        : segments[0];
                }
                catch
                {
                    return null;
                }
            }

            // Bare slug — accept if no spaces
            return input.Contains(' ') ? null : input;
        }
    }
}
