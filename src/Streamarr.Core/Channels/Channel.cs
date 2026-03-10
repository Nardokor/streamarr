using System;
using Streamarr.Core.Creators;
using Streamarr.Core.Datastore;

namespace Streamarr.Core.Channels
{
    public enum MembershipStatus
    {
        Unknown = 0,
        Active  = 1,
        None    = 2
    }

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

        // Display order (user-defined)
        public int SortOrder { get; set; }

        // State
        public bool Monitored { get; set; }
        public ChannelStatusType Status { get; set; }
        public DateTime? LastInfoSync { get; set; }
        public MembershipStatus MembershipStatus { get; set; }
        public DateTime? LastMembershipCheck { get; set; }

        // Wanted filter — content types
        public bool DownloadVideos { get; set; } = true;
        public bool DownloadShorts { get; set; } = true;
        public bool DownloadVods { get; set; }
        public bool DownloadLive { get; set; } = true;
        public bool DownloadMembers { get; set; }

        // Wanted filter — word filters
        public string WatchedWords { get; set; } = string.Empty;
        public string IgnoredWords { get; set; } = string.Empty;
        public bool WatchedDefeatsIgnored { get; set; } = true;

        // Download mode
        public bool AutoDownload { get; set; }

        // Retention
        public int? RetentionDays { get; set; }
        public bool KeepVideos { get; set; } = true;
        public bool KeepShorts { get; set; } = true;
        public bool KeepVods { get; set; }
        public bool KeepMembers { get; set; }
        public string RetentionKeepWords { get; set; } = string.Empty;

        // Navigation
        public LazyLoaded<Creator> Creator { get; set; }
    }
}
