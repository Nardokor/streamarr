using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using NLog;
using Streamarr.Core.Configuration;

namespace Streamarr.Core.MetadataSource.YouTube
{
    public interface IYouTubeApiClient
    {
        List<(string VideoId, DateTime PublishedAt)> GetPlaylistItems(string uploadsPlaylistId, DateTime? since = null);
        List<YoutubeVideo> GetVideoDetails(IEnumerable<string> videoIds);
    }

    public class YouTubeApiClient : IYouTubeApiClient
    {
        private const string BaseUrl = "https://www.googleapis.com/youtube/v3";

        private static readonly HttpClient _http = new HttpClient();
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IConfigService _config;
        private readonly Logger _logger;

        public YouTubeApiClient(IConfigService config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        public List<(string VideoId, DateTime PublishedAt)> GetPlaylistItems(string uploadsPlaylistId, DateTime? since = null)
        {
            var apiKey = _config.YouTubeApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("YouTube API key is not configured. Set it in Settings → YouTube.");
            }

            var results = new List<(string, DateTime)>();
            string pageToken = null;

            do
            {
                var url = $"{BaseUrl}/playlistItems?part=snippet&playlistId={Uri.EscapeDataString(uploadsPlaylistId)}&maxResults=50&key={Uri.EscapeDataString(apiKey)}";
                if (pageToken != null)
                {
                    url += $"&pageToken={Uri.EscapeDataString(pageToken)}";
                }

                var response = Fetch<YoutubePlaylistItemsResponse>(url);
                if (response?.Items == null)
                {
                    break;
                }

                var reachedSince = false;

                foreach (var item in response.Items)
                {
                    var videoId = item.Snippet?.ResourceId?.VideoId;
                    var publishedAtStr = item.Snippet?.PublishedAt;

                    if (string.IsNullOrWhiteSpace(videoId) || string.IsNullOrWhiteSpace(publishedAtStr))
                    {
                        continue;
                    }

                    if (!DateTime.TryParse(publishedAtStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var publishedAt))
                    {
                        continue;
                    }

                    publishedAt = publishedAt.ToUniversalTime();

                    if (since.HasValue && publishedAt <= since.Value)
                    {
                        reachedSince = true;
                        break;
                    }

                    results.Add((videoId, publishedAt));
                }

                if (reachedSince)
                {
                    break;
                }

                pageToken = response.NextPageToken;
            }
            while (pageToken != null);

            _logger.Debug("GetPlaylistItems: found {0} items for playlist {1}", results.Count, uploadsPlaylistId);
            return results;
        }

        public List<YoutubeVideo> GetVideoDetails(IEnumerable<string> videoIds)
        {
            var apiKey = _config.YouTubeApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("YouTube API key is not configured. Set it in Settings → YouTube.");
            }

            var idList = videoIds.ToList();
            var results = new List<YoutubeVideo>();

            foreach (var batch in idList.Chunk(50))
            {
                var ids = string.Join(",", batch.Select(Uri.EscapeDataString));
                var url = $"{BaseUrl}/videos?part=snippet,contentDetails,liveStreamingDetails&id={ids}&key={Uri.EscapeDataString(apiKey)}";

                var response = Fetch<YoutubeVideosResponse>(url);
                if (response?.Items != null)
                {
                    results.AddRange(response.Items);
                }
            }

            return results;
        }

        private T Fetch<T>(string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = _http.Send(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new InvalidOperationException(
                    $"YouTube API returned {(int)response.StatusCode}: {body}");
            }

            using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<T>(stream, _jsonOptions);
        }
    }
}
