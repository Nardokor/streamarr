using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Xml.Linq;
using NLog;

namespace Streamarr.Core.MetadataSource.YouTube
{
    public interface IYouTubeApiClient
    {
        List<(string VideoId, DateTime PublishedAt)> GetPlaylistItems(string apiKey, string uploadsPlaylistId, DateTime? since = null);
        List<YoutubeVideo> GetVideoDetails(string apiKey, IEnumerable<string> videoIds);
        void TestApiKey(string apiKey);
        string GetChannelThumbnailUrl(string apiKey, string channelId);

        // Fetches the channel's RSS feed and returns the most recent video IDs.
        // Free — no API quota consumed. Returns up to 5 IDs.
        List<string> GetChannelRecentVideoIds(string channelId);
    }

    public class YouTubeApiClient : IYouTubeApiClient
    {
        private const string BaseUrl = "https://www.googleapis.com/youtube/v3";

        private static readonly HttpClient _http = new HttpClient();
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Logger _logger;

        public YouTubeApiClient(Logger logger)
        {
            _logger = logger;
        }

        public List<(string VideoId, DateTime PublishedAt)> GetPlaylistItems(string apiKey, string uploadsPlaylistId, DateTime? since = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("YouTube API key is not configured. Set it in Settings → Sources → YouTube.");
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

                var foundNewInPage = false;

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
                        // Skip this item — it's older than our cutoff.
                        // Don't break: the playlist isn't guaranteed to be strictly ordered,
                        // so a single old item doesn't mean all subsequent items are also old.
                        continue;
                    }

                    results.Add((videoId, publishedAt));
                    foundNewInPage = true;
                }

                // Only stop paginating when an entire page contained nothing new.
                if (!foundNewInPage && since.HasValue)
                {
                    break;
                }

                pageToken = response.NextPageToken;
            }
            while (pageToken != null);

            _logger.Debug("GetPlaylistItems: found {0} items for playlist {1}", results.Count, uploadsPlaylistId);
            return results;
        }

        public List<YoutubeVideo> GetVideoDetails(string apiKey, IEnumerable<string> videoIds)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("YouTube API key is not configured. Set it in Settings → Sources → YouTube.");
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

        public void TestApiKey(string apiKey)
        {
            var url = $"{BaseUrl}/videos?part=snippet&id=dQw4w9WgXcQ&key={Uri.EscapeDataString(apiKey)}";
            Fetch<YoutubeVideosResponse>(url);
        }

        public List<string> GetChannelRecentVideoIds(string channelId)
        {
            var url = $"https://www.youtube.com/feeds/videos.xml?channel_id={Uri.EscapeDataString(channelId)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = _http.Send(request);

            if (!response.IsSuccessStatusCode)
            {
                return new List<string>();
            }

            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var doc = XDocument.Parse(content);
            XNamespace yt = "http://www.youtube.com/xml/schemas/2015";

            return doc.Descendants(yt + "videoId")
                      .Select(e => e.Value)
                      .Where(id => !string.IsNullOrWhiteSpace(id))
                      .Take(5)
                      .ToList();
        }

        public string GetChannelThumbnailUrl(string apiKey, string channelId)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return string.Empty;
            }

            var url = $"{BaseUrl}/channels?part=snippet&id={Uri.EscapeDataString(channelId)}&key={Uri.EscapeDataString(apiKey)}";
            var response = Fetch<YoutubeChannelsResponse>(url);
            var thumbnails = response?.Items?.FirstOrDefault()?.Snippet?.Thumbnails;

            var raw = thumbnails?.High?.Url
                ?? thumbnails?.Medium?.Url
                ?? thumbnails?.Default?.Url
                ?? string.Empty;

            return NormalizeThumbnailUrl(raw);
        }

        public static string NormalizeThumbnailUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }

            // yt3.ggpht.com is a legacy alias that 429s more aggressively; use the canonical host
            url = url.Replace("yt3.ggpht.com", "yt3.googleusercontent.com", StringComparison.OrdinalIgnoreCase);

            // Clamp the Google image-serving size parameter to something reasonable
            url = System.Text.RegularExpressions.Regex.Replace(url, @"=s\d+", "=s160");

            return url;
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
