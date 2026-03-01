using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Streamarr.Core.MetadataSource.Twitch
{
    public class TwitchTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public class TwitchUsersResponse
    {
        [JsonPropertyName("data")]
        public List<TwitchUser> Data { get; set; } = new List<TwitchUser>();
    }

    public class TwitchUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("login")]
        public string Login { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("profile_image_url")]
        public string ProfileImageUrl { get; set; }
    }

    public class TwitchSearchChannelsResponse
    {
        [JsonPropertyName("data")]
        public List<TwitchSearchChannel> Data { get; set; } = new List<TwitchSearchChannel>();
    }

    public class TwitchSearchChannel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("broadcaster_login")]
        public string BroadcasterLogin { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string ThumbnailUrl { get; set; }
    }

    public class TwitchVideosResponse
    {
        [JsonPropertyName("data")]
        public List<TwitchVideo> Data { get; set; } = new List<TwitchVideo>();

        [JsonPropertyName("pagination")]
        public TwitchPagination Pagination { get; set; }
    }

    public class TwitchVideo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("user_login")]
        public string UserLogin { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        // ISO-8601 timestamp
        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        // Contains %{width}x%{height} tokens — normalize before use
        [JsonPropertyName("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        // Twitch duration format e.g. "3h4m21s", "42m10s", "1h", "10s"
        [JsonPropertyName("duration")]
        public string Duration { get; set; }
    }

    public class TwitchStreamsResponse
    {
        [JsonPropertyName("data")]
        public List<TwitchStream> Data { get; set; } = new List<TwitchStream>();
    }

    public class TwitchStream
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("user_login")]
        public string UserLogin { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        // ISO-8601 timestamp
        [JsonPropertyName("started_at")]
        public string StartedAt { get; set; }

        // Contains {width}x{height} tokens — normalize before use
        [JsonPropertyName("thumbnail_url")]
        public string ThumbnailUrl { get; set; }
    }

    public class TwitchPagination
    {
        [JsonPropertyName("cursor")]
        public string Cursor { get; set; }
    }
}
