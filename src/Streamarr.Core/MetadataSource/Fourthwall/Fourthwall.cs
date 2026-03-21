#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation.Results;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;

namespace Streamarr.Core.MetadataSource.Fourthwall
{
    public class Fourthwall : MetadataSourceBase<FourthwallSettings>
    {
        // Matches the href on each video card in the listing page
        private static readonly Regex VideoEmbedHrefRegex = new Regex(
            @"href=""/supporters/video_embeds/(\d+)""",
            RegexOptions.Compiled);

        private static readonly Regex VideoTitleRegex = new Regex(
            @"<div class=""video__title"">\s*(.*?)\s*</div>",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex VideoInfoRegex = new Regex(
            @"<div class=""video__info"">\s*(.*?)\s*</div>",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex VideoImageRegex = new Regex(
            @"<img class=""video__image""[^>]+src=""([^""]+)""",
            RegexOptions.Compiled);

        // Matches all common YouTube URL formats embedded in post body anchor tags
        private static readonly Regex YouTubeIdRegex = new Regex(
            @"href=""https?://(?:www\.)?(?:youtube\.com/(?:watch\?v=|live/)|youtu\.be/)([\w\-]{11})[^""]*""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Matches the absolute date on the individual post page (both video and post layouts)
        private static readonly Regex AbsoluteDateRegex = new Regex(
            @"<div class=""(?:video-page__meta|post__meta post__meta--static)"">\s*([A-Za-z]+ \d{1,2}, \d{4})\s*</div>",
            RegexOptions.Compiled);

        private static readonly Regex OgImageRegex = new Regex(
            @"<meta[^>]+property=""og:image""[^>]+content=""([^""]+)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex OgImageReverseRegex = new Regex(
            @"<meta[^>]+content=""([^""]+)""[^>]+property=""og:image""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex OgTitleRegex = new Regex(
            @"<meta[^>]+property=""og:title""[^>]+content=""([^""]+)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex OgTitleReverseRegex = new Regex(
            @"<meta[^>]+content=""([^""]+)""[^>]+property=""og:title""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PageTitleRegex = new Regex(
            @"<title>([^<]+)</title>",
            RegexOptions.Compiled);

        private static readonly Regex RelativeDateRegex = new Regex(
            @"(\d+|an?)\s+(\w+)\s+ago",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IFourthwallApiClient _client;
        private readonly Logger _logger;

        public Fourthwall(IFourthwallApiClient client, Logger logger)
        {
            _client = client;
            _logger = logger;
        }

        public override string Name => "Fourthwall";
        public override PlatformType Platform => PlatformType.Fourthwall;

        // When UseYouTubeApi is enabled, delegate livestream status checks to the
        // YouTube source (Fourthwall content IDs are YouTube video IDs).
        public override PlatformType LivestreamDelegatePlatform =>
            Settings.UseYouTubeApi ? PlatformType.YouTube : PlatformType.Fourthwall;

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

            if (!query.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !query.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Please enter the full URL of the Fourthwall site (e.g., https://namijifreesia.party/)");
            }

            var baseUrl = NormalizeBaseUrl(query);
            _logger.Info("Looking up Fourthwall site: {0}", baseUrl);

            var html = _client.FetchHtml(Settings.CookiesFilePath, baseUrl);
            var domain = new Uri(baseUrl).Host;
            var title = ExtractPageTitle(html);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = DomainToTitle(domain);
            }

            var thumbnailUrl = ExtractThumbnailUrl(html);

            var channel = new ChannelMetadataResult
            {
                Platform = PlatformType.Fourthwall,
                PlatformId = domain,
                PlatformUrl = baseUrl,
                Title = title,
                Description = string.Empty,
                ThumbnailUrl = thumbnailUrl
            };

            return new CreatorMetadataResult
            {
                Name = title,
                Description = string.Empty,
                ThumbnailUrl = thumbnailUrl,
                Channels = new List<ChannelMetadataResult> { channel }
            };
        }

        public override ChannelMetadataResult GetChannelMetadata(string platformUrl)
        {
            var html = _client.FetchHtml(Settings.CookiesFilePath, platformUrl);
            var domain = new Uri(platformUrl).Host;
            var title = ExtractPageTitle(html);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = DomainToTitle(domain);
            }

            var thumbnailUrl = ExtractThumbnailUrl(html);

            return new ChannelMetadataResult
            {
                Platform = PlatformType.Fourthwall,
                PlatformId = domain,
                PlatformUrl = platformUrl,
                Title = title,
                Description = string.Empty,
                ThumbnailUrl = thumbnailUrl
            };
        }

        // ── Content sync ───────────────────────────────────────────────────────

        public override IEnumerable<ContentMetadataResult> GetNewContent(
            string platformUrl,
            string platformId,
            DateTime? since,
            bool checkMembership = false)
        {
            var baseUrl = platformUrl.TrimEnd('/');
            var listingUrl = $"{baseUrl}/supporters/videos/all";

            _logger.Info("Fetching Fourthwall video listing: {0}", listingUrl);
            var listingHtml = _client.FetchHtml(Settings.CookiesFilePath, listingUrl);

            _logger.Debug("Listing page HTML (first 2000 chars): {0}",
                listingHtml.Length > 2000 ? listingHtml.Substring(0, 2000) : listingHtml);

            var cards = ParseVideoCards(listingHtml);
            _logger.Info("Found {0} video card(s) on listing page", cards.Count);

            // On first sync (since=null) fetch all post pages.
            // On subsequent syncs, posts older than since-2days are skipped.
            // The 2-day buffer accounts for imprecision in relative date strings.
            var cutoff = since?.AddDays(-2);

            var results = new List<ContentMetadataResult>();

            foreach (var card in cards)
            {
                var estimatedDate = ParseRelativeDate(card.RelativeDate);

                if (cutoff.HasValue && estimatedDate.HasValue && estimatedDate.Value < cutoff.Value)
                {
                    // Listing is newest-first; first card past the cutoff means all remaining are older.
                    _logger.Debug(
                        "Post {0} estimated at {1} is older than cutoff {2} — stopping",
                        card.PostId,
                        estimatedDate.Value.ToString("u"),
                        cutoff.Value.ToString("u"));
                    break;
                }

                try
                {
                    var postUrl = $"{baseUrl}/supporters/posts/{card.PostId}";
                    var postHtml = _client.FetchHtml(Settings.CookiesFilePath, postUrl);

                    var youtubeId = ExtractYouTubeId(postHtml);
                    if (string.IsNullOrWhiteSpace(youtubeId))
                    {
                        _logger.Debug("No YouTube URL found in post {0} — skipping", card.PostId);
                        continue;
                    }

                    var absoluteDate = ParseAbsoluteDate(postHtml);

                    results.Add(new ContentMetadataResult
                    {
                        PlatformContentId = youtubeId,
                        PlatformChannelId = platformId,
                        PlatformChannelTitle = card.Title,
                        ContentType = ContentType.Video,
                        Title = card.Title,
                        Description = string.Empty,
                        ThumbnailUrl = card.ThumbnailUrl,
                        AirDateUtc = absoluteDate,
                        IsAccessible = true
                    });

                    _logger.Debug("Post {0}: YouTube ID={1}, Date={2}", card.PostId, youtubeId, absoluteDate?.ToString("u") ?? "unknown");
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to fetch post {0}", card.PostId);
                }
            }

            _logger.Info("GetNewContent for Fourthwall {0}: {1} item(s)", platformUrl, results.Count);
            return results;
        }

        // ── Single / batch lookup ──────────────────────────────────────────────

        public override ContentMetadataResult? GetContentMetadata(string platformContentId)
        {
            // Fourthwall content is identified by YouTube video ID;
            // individual lookup is not supported at this layer.
            return null;
        }

        public override IEnumerable<ContentMetadataResult> GetContentMetadataBatch(
            IEnumerable<string> platformContentIds)
        {
            return Enumerable.Empty<ContentMetadataResult>();
        }

        // ── Livestream status ──────────────────────────────────────────────────

        public override IEnumerable<ContentStatusUpdate> GetLivestreamStatusUpdates(
            IEnumerable<string> platformContentIds)
        {
            // Fourthwall videos are unlisted YouTube videos; live status is not tracked here.
            return Enumerable.Empty<ContentStatusUpdate>();
        }

        // ── Parsing helpers ────────────────────────────────────────────────────

        private sealed class VideoCard
        {
            public string PostId { get; }
            public string Title { get; }
            public string ThumbnailUrl { get; }
            public string RelativeDate { get; }

            public VideoCard(string postId, string title, string thumbnailUrl, string relativeDate)
            {
                PostId = postId;
                Title = title;
                ThumbnailUrl = thumbnailUrl;
                RelativeDate = relativeDate;
            }
        }

        private List<VideoCard> ParseVideoCards(string html)
        {
            var cards = new List<VideoCard>();

            // Split on the test ID attribute present on each video card anchor
            var segments = html.Split(
                new[] { "data-testid=\"VideoCatalog.Video\"" },
                StringSplitOptions.RemoveEmptyEntries);

            // First segment is content before the first card
            for (var i = 1; i < segments.Length; i++)
            {
                var seg = segments[i];

                var hrefMatch = VideoEmbedHrefRegex.Match(seg);
                if (!hrefMatch.Success)
                {
                    continue;
                }

                var postId = hrefMatch.Groups[1].Value;
                var title = VideoTitleRegex.Match(seg).Groups[1].Value.Trim();
                var relativeDate = VideoInfoRegex.Match(seg).Groups[1].Value.Trim();
                var thumbnailUrl = VideoImageRegex.Match(seg).Groups[1].Value;

                cards.Add(new VideoCard(postId, title, thumbnailUrl, relativeDate));
            }

            return cards;
        }

        private static string? ExtractYouTubeId(string html)
        {
            var match = YouTubeIdRegex.Match(html);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static DateTime? ParseAbsoluteDate(string html)
        {
            var match = AbsoluteDateRegex.Match(html);
            if (!match.Success)
            {
                return null;
            }

            if (DateTime.TryParseExact(
                match.Groups[1].Value.Trim(),
                new[] { "MMM d, yyyy", "MMMM d, yyyy" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var date))
            {
                return date.ToUniversalTime();
            }

            return null;
        }

        private static DateTime? ParseRelativeDate(string relativeDate)
        {
            if (string.IsNullOrWhiteSpace(relativeDate))
            {
                return null;
            }

            var text = relativeDate.Trim().ToLowerInvariant();
            var now = DateTime.UtcNow;

            if (text == "just now" || text == "moments ago")
            {
                return now;
            }

            if (text == "yesterday")
            {
                return now.AddDays(-1);
            }

            var match = RelativeDateRegex.Match(text);
            if (!match.Success)
            {
                return null;
            }

            var rawAmount = match.Groups[1].Value;
            var amount = rawAmount == "a" || rawAmount == "an" ? 1 : int.Parse(rawAmount);
            var unit = match.Groups[2].Value.TrimEnd('s'); // "days" → "day"

            return unit switch
            {
                "minute" => now.AddMinutes(-amount),
                "hour"   => now.AddHours(-amount),
                "day"    => now.AddDays(-amount),
                "week"   => now.AddDays(-amount * 7),
                "month"  => now.AddDays(-amount * 30),
                "year"   => now.AddDays(-amount * 365),
                _        => (DateTime?)null
            };
        }

        private static string NormalizeBaseUrl(string url)
        {
            var uri = new Uri(url);
            return $"{uri.Scheme}://{uri.Host}/";
        }

        private static string ExtractMetaProperty(string html, Regex forward, Regex reverse)
        {
            var m = forward.Match(html);
            if (m.Success)
            {
                return m.Groups[1].Value.Trim();
            }

            m = reverse.Match(html);
            return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
        }

        private static string ExtractPageTitle(string html)
        {
            var title = ExtractMetaProperty(html, OgTitleRegex, OgTitleReverseRegex);
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }

            var pageTitle = PageTitleRegex.Match(html);
            return pageTitle.Success ? pageTitle.Groups[1].Value.Trim() : string.Empty;
        }

        private static string ExtractThumbnailUrl(string html)
            => ExtractMetaProperty(html, OgImageRegex, OgImageReverseRegex);

        private static string DomainToTitle(string domain)
        {
            if (domain.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                return domain.Substring(4);
            }

            return domain;
        }
    }
}
