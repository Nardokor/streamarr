using System;
using Streamarr.Core.Creators;
using Streamarr.Core.Datastore;

namespace Streamarr.Core.Channels
{
    public class Channel : ModelBase
    {
        // Relationship
        public int CreatorId { get; set; }

        // Platform identity
        public PlatformType Platform { get; set; }
        public string PlatformId { get; set; } = string.Empty;
        public string PlatformUrl { get; set; } = string.Empty;

        // Display metadata
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;

        // State
        public bool Monitored { get; set; }
        public ChannelStatusType Status { get; set; }
        public DateTime? LastInfoSync { get; set; }

        // Download filters
        public bool DownloadVideos { get; set; } = true;
        public bool DownloadShorts { get; set; } = true;
        public bool DownloadLivestreams { get; set; } = true;
        public string TitleFilter { get; set; } = string.Empty;

        // Archival
        public string PriorityFilter { get; set; } = string.Empty;
        public int? RetentionDays { get; set; }

        // Navigation
        public LazyLoaded<Creator> Creator { get; set; }
    }
}
