#nullable enable
using System;
using System.Collections.Generic;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.MetadataSource
{
    public interface IMetadataSource : IProvider
    {
        PlatformType Platform { get; }

        // When a platform's content IDs are native to a different platform (e.g. Fourthwall
        // hosts unlisted YouTube videos), this returns that platform so callers can route
        // livestream status checks through the correct source.  Defaults to Platform.
        PlatformType LivestreamDelegatePlatform { get; }

        // Creator / channel discovery
        CreatorMetadataResult SearchCreator(string query);
        ChannelMetadataResult GetChannelMetadata(string platformUrl);

        // Content sync — checkMembership=true fetches the membership/subscriber tab if the platform supports it
        IEnumerable<ContentMetadataResult> GetNewContent(string platformUrl, string platformId, DateTime? since, bool checkMembership = false);

        // Single-item and batch lookup (null = deleted from platform)
        ContentMetadataResult? GetContentMetadata(string platformContentId);
        IEnumerable<ContentMetadataResult> GetContentMetadataBatch(IEnumerable<string> platformContentIds);

        // Livestream tracking — returns empty enumerable on platforms without live support
        IEnumerable<ContentStatusUpdate> GetLivestreamStatusUpdates(IEnumerable<string> platformContentIds);

        // Probe whether authenticated user can access a given piece of content (e.g. members-only).
        // withCookies=false probes without auth to discover the required tier; exit non-zero
        // always returns tier info when no cookies are present.
        ContentAccessibilityResult ProbeContentAccessibility(string platformContentId, bool withCookies = true);

        // Check whether the channel is currently hosting an active live stream.
        // Returns metadata for the live content if found, or null if not live.
        ContentMetadataResult? GetActiveLivestream(string platformUrl, string platformId);

        // Returns the URL yt-dlp should fetch to download the given piece of content.
        string GetDownloadUrl(string platformContentId);
    }

    // ── Result DTOs ──────────────────────────────────────────────────────────

    public class CreatorMetadataResult
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public List<ChannelMetadataResult> Channels { get; set; } = new List<ChannelMetadataResult>();
        public int? ExistingCreatorId { get; set; }
    }

    public class ChannelMetadataResult
    {
        public PlatformType Platform { get; set; }
        public string PlatformId { get; set; } = string.Empty;
        public string PlatformUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class ContentMetadataResult
    {
        public string PlatformContentId { get; set; } = string.Empty;
        public string PlatformChannelId { get; set; } = string.Empty;
        public string PlatformChannelTitle { get; set; } = string.Empty;
        public ContentType ContentType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public TimeSpan? Duration { get; set; }
        public DateTime? AirDateUtc { get; set; }
        public bool IsMembers { get; set; }
        public bool IsAccessible { get; set; } = true;
    }

    public class ContentStatusUpdate
    {
        public string PlatformContentId { get; set; } = string.Empty;
        public ContentType NewContentType { get; set; }
        public DateTime? NewAirDateUtc { get; set; }
        public bool ExistsOnPlatform { get; set; } = true;
        public bool ShouldTriggerDownload { get; set; }
    }

    // Result of a per-video accessibility probe (members-only content).
    // Distinguishes between three outcomes so callers can short-circuit or
    // group by tier rather than probing every inaccessible video individually.
    public struct ContentAccessibilityResult
    {
        public bool IsAccessible { get; }

        // True when the authenticated user has no membership at all ("Join this channel…").
        public bool IsNotMember { get; }

        // Non-empty when the user has a membership but the video requires a higher tier.
        public string RequiredTier { get; }

        // True when yt-dlp was rate-limited by YouTube — result is transient and should
        // not be used to update stored accessibility state.
        public bool IsRateLimited { get; }

        private ContentAccessibilityResult(bool accessible, bool notMember, string requiredTier, bool rateLimited)
        {
            IsAccessible = accessible;
            IsNotMember = notMember;
            RequiredTier = requiredTier;
            IsRateLimited = rateLimited;
        }

        public static ContentAccessibilityResult Accessible() =>
            new ContentAccessibilityResult(true, false, string.Empty, false);

        public static ContentAccessibilityResult NotMember() =>
            new ContentAccessibilityResult(false, true, string.Empty, false);

        public static ContentAccessibilityResult TierRequired(string tier) =>
            new ContentAccessibilityResult(false, false, tier ?? string.Empty, false);

        public static ContentAccessibilityResult Inaccessible() =>
            new ContentAccessibilityResult(false, false, string.Empty, false);

        public static ContentAccessibilityResult RateLimited() =>
            new ContentAccessibilityResult(false, false, string.Empty, true);
    }
}
