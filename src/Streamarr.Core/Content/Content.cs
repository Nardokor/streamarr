using System;
using Streamarr.Core.Channels;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Datastore;

namespace Streamarr.Core.Content
{
    public class Content : ModelBase
    {
        // Relationships
        public int ChannelId { get; set; }
        public int ContentFileId { get; set; }

        // Platform identity
        public string PlatformContentId { get; set; } = string.Empty;
        public ContentType ContentType { get; set; }

        // Display metadata
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public TimeSpan? Duration { get; set; }

        // Dates
        public DateTime? AirDateUtc { get; set; }
        public DateTime DateAdded { get; set; }

        // State
        public bool Monitored { get; set; }
        public bool IsMembers { get; set; }
        public bool IsAccessible { get; set; } = true;

        // The membership tier required to access this video, as returned by the platform
        // when a probe fails (e.g. "Double-Decker Bus"). Empty for non-members or public content.
        // Used to group inaccessible videos by tier so only one probe is needed per tier.
        public string MembershipTier { get; set; } = string.Empty;

        public ContentStatus Status { get; set; }
        public ContentStatus? PreviousStatus { get; set; }

        // Navigation
        public LazyLoaded<Channel> Channel { get; set; }
        public LazyLoaded<ContentFile> ContentFile { get; set; }
    }
}
