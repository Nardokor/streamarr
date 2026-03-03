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

        // Creator / channel discovery
        CreatorMetadataResult SearchCreator(string query);
        ChannelMetadataResult GetChannelMetadata(string platformUrl);

        // Content sync
        IEnumerable<ContentMetadataResult> GetNewContent(string platformUrl, string platformId, DateTime? since);

        // Single-item and batch lookup (null = deleted from platform)
        ContentMetadataResult? GetContentMetadata(string platformContentId);
        IEnumerable<ContentMetadataResult> GetContentMetadataBatch(IEnumerable<string> platformContentIds);

        // Livestream tracking — returns empty enumerable on platforms without live support
        IEnumerable<ContentStatusUpdate> GetLivestreamStatusUpdates(IEnumerable<string> platformContentIds);

        // Probe whether authenticated user can access a given piece of content (e.g. members-only)
        bool ProbeContentAccessibility(string platformContentId);
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
}
