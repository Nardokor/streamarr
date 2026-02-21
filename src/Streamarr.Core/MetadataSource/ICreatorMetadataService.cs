using System;
using System.Collections.Generic;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;

namespace Streamarr.Core.MetadataSource
{
    public interface ICreatorMetadataService
    {
        CreatorMetadataResult SearchCreator(string query);
        ChannelMetadataResult GetChannelMetadata(string platformUrl);
        List<ContentMetadataResult> GetNewContent(string platformUrl, string platformId, DateTime? since = null);
    }

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
        public ContentType ContentType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public TimeSpan? Duration { get; set; }
        public DateTime? AirDateUtc { get; set; }
    }
}
