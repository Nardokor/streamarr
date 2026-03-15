using System;
using Streamarr.Core.Datastore;

namespace Streamarr.Core.Import
{
    public enum UnmatchedFileReason
    {
        NoYouTubeId = 0,
        NoMetadataSource = 1,
        MetadataNotFound = 2,
        NoChannelId = 3,
    }

    public class UnmatchedFile : ModelBase
    {
        public int CreatorId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime DateFound { get; set; }
        public UnmatchedFileReason Reason { get; set; }
    }
}
