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

        // Wanted filter — content types
        public bool DownloadVideos { get; set; }
        public bool DownloadShorts { get; set; }
        public bool DownloadVods { get; set; }
        public bool DownloadLive { get; set; } = true;

        // Wanted filter — word filters
        public string WatchedWords { get; set; } = string.Empty;
        public string IgnoredWords { get; set; } = string.Empty;
        public bool WatchedDefeatsIgnored { get; set; } = true;

        // Download mode
        public bool AutoDownload { get; set; }

        // Retention
        public int? RetentionDays { get; set; }
        public bool RetentionVideos { get; set; }
        public bool RetentionShorts { get; set; }
        public bool RetentionVods { get; set; }
        public bool RetentionLive { get; set; }
        public string RetentionExceptionWords { get; set; } = string.Empty;

        // Navigation
        public LazyLoaded<Creator> Creator { get; set; }
    }
}
