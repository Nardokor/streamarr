using System;
using System.Collections.Generic;
using Streamarr.Core.Datastore;
using Streamarr.Core.Profiles.Qualities;

namespace Streamarr.Core.Creators
{
    public class Creator : ModelBase
    {
        // Display metadata
        public string Title { get; set; } = string.Empty;
        public string CleanTitle { get; set; } = string.Empty;
        public string SortTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;

        // Organization
        public string Path { get; set; } = string.Empty;
        public string RootFolderPath { get; set; } = string.Empty;
        public int QualityProfileId { get; set; }
        public HashSet<int> Tags { get; set; }

        // State
        public bool Monitored { get; set; }
        public CreatorStatusType Status { get; set; }
        public DateTime Added { get; set; }
        public DateTime? LastInfoSync { get; set; }

        // Navigation
        public LazyLoaded<QualityProfile> QualityProfile { get; set; }

        public Creator()
        {
            Tags = new HashSet<int>();
        }
    }
}
