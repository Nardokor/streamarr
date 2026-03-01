using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using NLog;

namespace Streamarr.Core.MetadataSource.Twitch
{
    public class TwitchApiClient : ITwitchApiClient
    {
        private const string HelixBaseUrl = "https://api.twitch.tv/helix";
        private const string AuthBaseUrl  = "https://id.twitch.tv/oauth2";

        private static readonly HttpClient _http = new HttpClient();
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly Regex _durationRegex = new Regex(
            @"(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s)?",
            RegexOptions.Compiled);

        private readonly Logger _logger;

        public TwitchApiClient(Logger logger)
        {
            _logger = logger;
        }

        // ── Token ──────────────────────────────────────────────────────────────

        public string GetAccessToken(string clientId, string clientSecret)
        {
            var url = $"{AuthBaseUrl}/token" +
                      $"?client_id={Uri.EscapeDataString(clientId)}" +
                      $"&client_secret={Uri.EscapeDataString(clientSecret)}" +
                      $"&grant_type=client_credentials";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            using var response = _http.Send(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new InvalidOperationException(
                    $"Twitch token request failed ({(int)response.StatusCode}): {body}");
            }

            using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            var tokenResponse = JsonSerializer.Deserialize<TwitchTokenResponse>(stream, _jsonOptions);

            if (tokenResponse?.AccessToken == null)
            {
                throw new InvalidOperationException("Twitch token response contained no access_token.");
            }

            return tokenResponse.AccessToken;
        }

        public void TestCredentials(string clientId, string clientSecret)
        {
            var token = GetAccessToken(clientId, clientSecret);
            GetUserByLogin(clientId, token, "twitch");
        }

        // ── User lookup ────────────────────────────────────────────────────────

        public TwitchUser GetUserByLogin(string clientId, string accessToken, string login)
        {
            var url = $"{HelixBaseUrl}/users?login={Uri.EscapeDataString(login)}";
            var response = FetchHelix<TwitchUsersResponse>(clientId, accessToken, url);
            return response?.Data?.FirstOrDefault();
        }

        public TwitchUser GetUserById(string clientId, string accessToken, string userId)
        {
            var url = $"{HelixBaseUrl}/users?id={Uri.EscapeDataString(userId)}";
            var response = FetchHelix<TwitchUsersResponse>(clientId, accessToken, url);
            return response?.Data?.FirstOrDefault();
        }

        // ── Search ─────────────────────────────────────────────────────────────

        public List<TwitchSearchChannel> SearchChannels(string clientId, string accessToken, string query, int first = 5)
        {
            var url = $"{HelixBaseUrl}/search/channels" +
                      $"?query={Uri.EscapeDataString(query)}&first={first}";
            var response = FetchHelix<TwitchSearchChannelsResponse>(clientId, accessToken, url);
            return response?.Data ?? new List<TwitchSearchChannel>();
        }

        // ── VODs ───────────────────────────────────────────────────────────────

        public List<TwitchVideo> GetVideos(string clientId, string accessToken, string userId, DateTime? since = null)
        {
            var results = new List<TwitchVideo>();
            string cursor = null;

            do
            {
                var url = $"{HelixBaseUrl}/videos?user_id={Uri.EscapeDataString(userId)}" +
                          "&type=archive&sort=time&first=100";
                if (cursor != null)
                {
                    url += $"&after={Uri.EscapeDataString(cursor)}";
                }

                var response = FetchHelix<TwitchVideosResponse>(clientId, accessToken, url);

                if (response?.Data == null || response.Data.Count == 0)
                {
                    break;
                }

                var doneEarly = false;

                foreach (var video in response.Data)
                {
                    if (!DateTime.TryParse(video.CreatedAt, null, DateTimeStyles.RoundtripKind, out var createdAt))
                    {
                        results.Add(video);
                        continue;
                    }

                    createdAt = createdAt.ToUniversalTime();

                    if (since.HasValue && createdAt <= since.Value)
                    {
                        // Results are newest-first; first item older than since means
                        // everything remaining is also older — stop paginating.
                        doneEarly = true;
                        break;
                    }

                    results.Add(video);
                }

                if (doneEarly)
                {
                    break;
                }

                cursor = response.Pagination?.Cursor;
            }
            while (cursor != null);

            _logger.Debug("Twitch GetVideos for user {0}: {1} VODs (since: {2})", userId, results.Count, since?.ToString("u") ?? "all");

            return results;
        }

        // ── Single video ───────────────────────────────────────────────────────

        public TwitchVideo GetVideo(string clientId, string accessToken, string videoId)
        {
            var url = $"{HelixBaseUrl}/videos?id={Uri.EscapeDataString(videoId)}";
            var response = FetchHelix<TwitchVideosResponse>(clientId, accessToken, url);
            return response?.Data?.FirstOrDefault();
        }

        // ── Live stream ────────────────────────────────────────────────────────

        public TwitchStream GetLiveStream(string clientId, string accessToken, string userLogin)
        {
            var url = $"{HelixBaseUrl}/streams?user_login={Uri.EscapeDataString(userLogin)}";
            var response = FetchHelix<TwitchStreamsResponse>(clientId, accessToken, url);
            return response?.Data?.FirstOrDefault();
        }

        // ── HTTP helper ────────────────────────────────────────────────────────

        private T FetchHelix<T>(string clientId, string accessToken, string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Client-Id", clientId);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            using var response = _http.Send(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new InvalidOperationException(
                    $"Twitch API returned {(int)response.StatusCode} for {url}: {body}");
            }

            using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<T>(stream, _jsonOptions);
        }

        // ── Static utilities ───────────────────────────────────────────────────

        // Parses Twitch duration strings like "3h4m21s", "42m10s", "1h", "10s".
        public static TimeSpan? ParseTwitchDuration(string duration)
        {
            if (string.IsNullOrWhiteSpace(duration))
            {
                return null;
            }

            var match = _durationRegex.Match(duration);
            if (!match.Success)
            {
                return null;
            }

            var hasHours   = !string.IsNullOrEmpty(match.Groups[1].Value);
            var hasMinutes = !string.IsNullOrEmpty(match.Groups[2].Value);
            var hasSeconds = !string.IsNullOrEmpty(match.Groups[3].Value);

            if (!hasHours && !hasMinutes && !hasSeconds)
            {
                return null;
            }

            var hours   = hasHours   ? int.Parse(match.Groups[1].Value) : 0;
            var minutes = hasMinutes ? int.Parse(match.Groups[2].Value) : 0;
            var seconds = hasSeconds ? int.Parse(match.Groups[3].Value) : 0;

            return new TimeSpan(hours, minutes, seconds);
        }

        // Normalizes Twitch thumbnail URL token placeholders to a fixed 320×180 size.
        // Videos use %{width}x%{height}; streams use {width}x{height}.
        public static string NormalizeThumbnailUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }

            url = url
                .Replace("%{width}",  "320", StringComparison.OrdinalIgnoreCase)
                .Replace("%{height}", "180", StringComparison.OrdinalIgnoreCase);

            url = Regex.Replace(url, @"\{width\}x\{height\}", "320x180");

            return url;
        }
    }
}
