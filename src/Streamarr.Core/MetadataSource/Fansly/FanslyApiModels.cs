using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Streamarr.Core.MetadataSource.Fansly
{
    // ── Account ───────────────────────────────────────────────────────────────

    public class FanslyAccountResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("response")]
        public List<FanslyAccount> Response { get; set; } = new List<FanslyAccount>();
    }

    public class FanslyAccount
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("about")]
        public string About { get; set; }

        [JsonPropertyName("location")]
        public string Location { get; set; }

        [JsonPropertyName("followCount")]
        public int FollowCount { get; set; }

        [JsonPropertyName("avatar")]
        public FanslyMedia Avatar { get; set; }
    }

    // ── Timeline ──────────────────────────────────────────────────────────────

    public class FanslyTimelineResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("response")]
        public FanslyTimelinePayload Response { get; set; }
    }

    public class FanslyTimelinePayload
    {
        [JsonPropertyName("posts")]
        public List<FanslyPost> Posts { get; set; } = new List<FanslyPost>();

        [JsonPropertyName("accountMedia")]
        public List<FanslyAccountMedia> AccountMedia { get; set; } = new List<FanslyAccountMedia>();

        [JsonPropertyName("accounts")]
        public List<FanslyAccount> Accounts { get; set; } = new List<FanslyAccount>();

        // Cursor for the next page (pass as `before` param). Null when no more pages.
        [JsonPropertyName("nextBeforeId")]
        public string NextBeforeId { get; set; }
    }

    // ── Post ──────────────────────────────────────────────────────────────────

    public class FanslyPostResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("response")]
        public FanslyTimelinePayload Response { get; set; }
    }

    public class FanslyPost
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("accountId")]
        public string AccountId { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("createdAt")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("attachments")]
        public List<FanslyAttachment> Attachments { get; set; } = new List<FanslyAttachment>();
    }

    public class FanslyAttachment
    {
        [JsonPropertyName("contentId")]
        public string ContentId { get; set; }

        [JsonPropertyName("pos")]
        public int Pos { get; set; }
    }

    // ── Media ─────────────────────────────────────────────────────────────────

    public class FanslyAccountMedia
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("accountId")]
        public string AccountId { get; set; }

        [JsonPropertyName("mediaId")]
        public string MediaId { get; set; }

        [JsonPropertyName("createdAt")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("media")]
        public FanslyMedia Media { get; set; }
    }

    public class FanslyMedia
    {
        // type 2 = video, type 1 = image
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("accountId")]
        public string AccountId { get; set; }

        [JsonPropertyName("mimetype")]
        public string Mimetype { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("createdAt")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public long UpdatedAt { get; set; }

        [JsonPropertyName("metadata")]
        public string Metadata { get; set; }

        [JsonPropertyName("locations")]
        public List<FanslyLocation> Locations { get; set; } = new List<FanslyLocation>();

        [JsonPropertyName("variants")]
        public List<FanslyMediaVariant> Variants { get; set; } = new List<FanslyMediaVariant>();
    }

    public class FanslyMediaVariant
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("mimetype")]
        public string Mimetype { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("locations")]
        public List<FanslyLocation> Locations { get; set; } = new List<FanslyLocation>();
    }

    public class FanslyLocation
    {
        [JsonPropertyName("location")]
        public string Location { get; set; }
    }
}
