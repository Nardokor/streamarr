using System.Text.Json.Serialization;

namespace Streamarr.Core.Download.YtDlp
{
    public class YtDlpVideoInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; } = string.Empty;

        [JsonPropertyName("duration")]
        public double? Duration { get; set; }

        [JsonPropertyName("upload_date")]
        public string UploadDate { get; set; } = string.Empty;

        [JsonPropertyName("channel")]
        public string Channel { get; set; } = string.Empty;

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; } = string.Empty;

        [JsonPropertyName("channel_url")]
        public string ChannelUrl { get; set; } = string.Empty;

        [JsonPropertyName("uploader_url")]
        public string UploaderUrl { get; set; } = string.Empty;

        [JsonPropertyName("webpage_url")]
        public string WebpageUrl { get; set; } = string.Empty;

        [JsonPropertyName("view_count")]
        public long? ViewCount { get; set; }

        [JsonPropertyName("is_live")]
        public bool? IsLive { get; set; }

        [JsonPropertyName("was_live")]
        public bool? WasLive { get; set; }
    }
}
