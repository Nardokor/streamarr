using System;
using System.Collections.Generic;
using Streamarr.Core.Datastore;

namespace Streamarr.Core.Channels
{
    public class Channel : ModelBase
    {
        public string Title { get; set; }
        public string PlatformUrl { get; set; }
        public string PlatformId { get; set; }
        public string Path { get; set; }
        public bool Monitored { get; set; }
        public string RootFolderPath { get; set; }
        public int QualityProfileId { get; set; }
        public HashSet<int> Tags { get; set; }
        public DateTime Added { get; set; }
    }
}
