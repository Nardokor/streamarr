using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Streamarr.Core.Download.YtDlp
{
    public class YtDlpChannelInfo
    {
        [JsonPropertyName("channel")]
        public string Channel { get; set; } = string.Empty;

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; } = string.Empty;

        [JsonPropertyName("channel_url")]
        public string ChannelUrl { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; } = string.Empty;

        [JsonPropertyName("uploader")]
        public string Uploader { get; set; } = string.Empty;

        [JsonPropertyName("uploader_id")]
        public string UploaderId { get; set; } = string.Empty;

        [JsonPropertyName("uploader_url")]
        public string UploaderUrl { get; set; } = string.Empty;

        [JsonPropertyName("entries")]
        public List<YtDlpVideoInfo> Entries { get; set; } = new List<YtDlpVideoInfo>();
    }
}
