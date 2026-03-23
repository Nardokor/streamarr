using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Streamarr.Core.Download.YtDlp
{
    public class YtDlpThumbnailEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }
    }

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

        [JsonPropertyName("thumbnails")]
        public List<YtDlpThumbnailEntry> Thumbnails { get; set; } = new List<YtDlpThumbnailEntry>();

        [JsonPropertyName("uploader")]
        public string Uploader { get; set; } = string.Empty;

        [JsonPropertyName("uploader_id")]
        public string UploaderId { get; set; } = string.Empty;

        [JsonPropertyName("uploader_url")]
        public string UploaderUrl { get; set; } = string.Empty;

        [JsonPropertyName("entries")]
        public List<YtDlpVideoInfo> Entries { get; set; } = new List<YtDlpVideoInfo>();

        /// <summary>
        /// Returns the best avatar URL from the thumbnails array, falling back to
        /// the singular thumbnail field. yt-dlp populates the thumbnails array with
        /// channel-level images (avatar, banner) when fetching a channel page; the
        /// singular thumbnail field often resolves to the first video's thumbnail
        /// instead of the channel avatar.
        /// </summary>
        public string BestAvatarUrl
        {
            get
            {
                // Prefer entries whose id contains "avatar" (yt-dlp labels these explicitly)
                var avatar = Thumbnails.FirstOrDefault(t =>
                    !string.IsNullOrEmpty(t.Id) &&
                    t.Id.Contains("avatar", System.StringComparison.OrdinalIgnoreCase));

                if (avatar != null && !string.IsNullOrEmpty(avatar.Url))
                {
                    return avatar.Url;
                }

                // Fall back to the largest thumbnail by pixel count
                var largest = Thumbnails
                    .Where(t => !string.IsNullOrEmpty(t.Url) && t.Height.HasValue && t.Width.HasValue)
                    .OrderByDescending(t => t.Height!.Value * t.Width!.Value)
                    .FirstOrDefault();

                if (largest != null && !string.IsNullOrEmpty(largest.Url))
                {
                    return largest.Url;
                }

                // Last resort: singular thumbnail field
                return Thumbnail;
            }
        }
    }
}
