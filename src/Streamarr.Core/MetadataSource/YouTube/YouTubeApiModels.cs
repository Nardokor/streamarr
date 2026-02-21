using System.Collections.Generic;

namespace Streamarr.Core.MetadataSource.YouTube
{
    public class YoutubePlaylistItemsResponse
    {
        public string NextPageToken { get; set; }
        public List<YoutubePlaylistItem> Items { get; set; } = new();
    }

    public class YoutubePlaylistItem
    {
        public YoutubePlaylistItemSnippet Snippet { get; set; }
    }

    public class YoutubePlaylistItemSnippet
    {
        public string PublishedAt { get; set; }
        public YoutubeResourceId ResourceId { get; set; }
    }

    public class YoutubeResourceId
    {
        public string VideoId { get; set; }
    }

    public class YoutubeVideosResponse
    {
        public List<YoutubeVideo> Items { get; set; } = new();
    }

    public class YoutubeVideo
    {
        public string Id { get; set; }
        public YoutubeVideoSnippet Snippet { get; set; }
        public YoutubeVideoContentDetails ContentDetails { get; set; }
        public YoutubeVideoLiveStreamingDetails LiveStreamingDetails { get; set; }
    }

    public class YoutubeVideoSnippet
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public YoutubeVideoThumbnails Thumbnails { get; set; }
    }

    public class YoutubeVideoThumbnails
    {
        public YoutubeVideoThumbnail Medium { get; set; }
        public YoutubeVideoThumbnail High { get; set; }
    }

    public class YoutubeVideoThumbnail
    {
        public string Url { get; set; }
    }

    public class YoutubeVideoContentDetails
    {
        public string Duration { get; set; }
    }

    public class YoutubeVideoLiveStreamingDetails
    {
    }

    public class YoutubeChannelsResponse
    {
        public List<YoutubeChannel> Items { get; set; } = new();
    }

    public class YoutubeChannel
    {
        public YoutubeChannelSnippet Snippet { get; set; }
    }

    public class YoutubeChannelSnippet
    {
        public YoutubeChannelThumbnails Thumbnails { get; set; }
    }

    public class YoutubeChannelThumbnails
    {
        public YoutubeVideoThumbnail Default { get; set; }
        public YoutubeVideoThumbnail Medium { get; set; }
        public YoutubeVideoThumbnail High { get; set; }
    }
}
