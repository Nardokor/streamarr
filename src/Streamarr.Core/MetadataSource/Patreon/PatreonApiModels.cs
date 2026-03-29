using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Streamarr.Core.MetadataSource.Patreon
{
    // ── Top-level response wrappers ───────────────────────────────────────────

    public class PatreonDataResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }
    }

    public class PatreonListResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = new List<T>();

        [JsonPropertyName("links")]
        public PatreonLinks Links { get; set; }
    }

    public class PatreonLinks
    {
        [JsonPropertyName("next")]
        public string Next { get; set; }
    }

    // ── Campaign ──────────────────────────────────────────────────────────────

    public class PatreonCampaignResource
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("attributes")]
        public PatreonCampaignAttributes Attributes { get; set; }
    }

    public class PatreonCampaignAttributes
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("creation_name")]
        public string CreationName { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("vanity")]
        public string Vanity { get; set; }

        [JsonPropertyName("patron_count")]
        public int PatronCount { get; set; }
    }

    // ── Post ──────────────────────────────────────────────────────────────────

    public class PatreonPostResource
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("attributes")]
        public PatreonPostAttributes Attributes { get; set; }
    }

    public class PatreonPostAttributes
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("published_at")]
        public string PublishedAt { get; set; }

        [JsonPropertyName("post_type")]
        public string PostType { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; }

        // Present on link and video_external_file posts
        [JsonPropertyName("embed")]
        public PatreonEmbed Embed { get; set; }
    }

    public class PatreonEmbed
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }
    }
}
