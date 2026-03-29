using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using NLog;

namespace Streamarr.Core.MetadataSource.Patreon
{
    public interface IPatreonApiClient
    {
        PatreonCampaignResource GetCampaignByVanity(string cookiesFilePath, string vanity);
        List<PatreonPostResource> GetCampaignPosts(string cookiesFilePath, string campaignId, DateTime? since);
        PatreonPostResource GetPost(string cookiesFilePath, string postId);
    }

    public class PatreonApiClient : IPatreonApiClient
    {
        // Patreon's internal JSON API — same endpoints yt-dlp uses, no OAuth required.
        private const string ApiBase = "https://www.patreon.com/api";

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Logger _logger;

        public PatreonApiClient(Logger logger)
        {
            _logger = logger;
        }

        // Looks up a campaign by its vanity slug (e.g. "creatorname").
        public PatreonCampaignResource GetCampaignByVanity(string cookiesFilePath, string vanity)
        {
            var url = $"{ApiBase}/campaigns" +
                      $"?filter[vanity]={Uri.EscapeDataString(vanity)}" +
                      "&fields[campaign]=name,creation_name,summary,url,image_url,vanity,patron_count";

            var response = Fetch<PatreonListResponse<PatreonCampaignResource>>(cookiesFilePath, url);
            return response?.Data?.Count > 0 ? response.Data[0] : null;
        }

        // Returns all posts for a campaign published after `since`, newest-first.
        public List<PatreonPostResource> GetCampaignPosts(string cookiesFilePath, string campaignId, DateTime? since)
        {
            var results = new List<PatreonPostResource>();
            var url = $"{ApiBase}/posts" +
                      $"?filter[campaign_id]={Uri.EscapeDataString(campaignId)}" +
                      "&sort=-published_at" +
                      "&page[count]=50" +
                      "&fields[post]=title,content,url,published_at,post_type,thumbnail_url,is_public,embed";

            while (url != null)
            {
                var response = Fetch<PatreonListResponse<PatreonPostResource>>(cookiesFilePath, url);
                if (response?.Data == null || response.Data.Count == 0)
                {
                    break;
                }

                var doneEarly = false;
                foreach (var post in response.Data)
                {
                    if (!TryParsePublishedAt(post.Attributes?.PublishedAt, out var publishedAt))
                    {
                        results.Add(post);
                        continue;
                    }

                    if (since.HasValue && publishedAt <= since.Value)
                    {
                        // Results are newest-first; first item at or before since means all remaining are also older.
                        doneEarly = true;
                        break;
                    }

                    results.Add(post);
                }

                if (doneEarly)
                {
                    break;
                }

                url = response.Links?.Next;
            }

            _logger.Debug("Patreon GetCampaignPosts for {0}: {1} posts (since: {2})", campaignId, results.Count, since?.ToString("u") ?? "all");
            return results;
        }

        // Fetches a single post by ID.
        public PatreonPostResource GetPost(string cookiesFilePath, string postId)
        {
            var url = $"{ApiBase}/posts/{Uri.EscapeDataString(postId)}" +
                      "?fields[post]=title,content,url,published_at,post_type,thumbnail_url,is_public,embed";

            var response = Fetch<PatreonDataResponse<PatreonPostResource>>(cookiesFilePath, url);
            return response?.Data;
        }

        // ── HTTP helper ────────────────────────────────────────────────────────

        private T Fetch<T>(string cookiesFilePath, string url)
        {
            var cookieContainer = LoadCookies(cookiesFilePath);
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                CheckCertificateRevocationList = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            client.DefaultRequestHeaders.Add("Referer", "https://www.patreon.com/");

            using var response = client.GetAsync(url).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new InvalidOperationException(
                    $"Patreon API returned {(int)response.StatusCode} for {url}: {body}");
            }

            using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<T>(stream, _jsonOptions);
        }

        private CookieContainer LoadCookies(string cookiesFilePath)
        {
            var container = new CookieContainer();

            if (string.IsNullOrWhiteSpace(cookiesFilePath) || !File.Exists(cookiesFilePath))
            {
                _logger.Warn("Patreon cookies file not found: {0}", cookiesFilePath);
                return container;
            }

            foreach (var line in File.ReadAllLines(cookiesFilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                var parts = line.Split('\t');
                if (parts.Length < 7)
                {
                    continue;
                }

                var domain = parts[0].TrimStart('.');
                var path = parts[2];
                var secure = parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                var name = parts[5];
                var value = parts[6];

                try
                {
                    var scheme = secure ? "https" : "http";
                    container.Add(
                        new Uri($"{scheme}://{domain}"),
                        new Cookie(name, value, path, domain));
                }
                catch (Exception ex)
                {
                    _logger.Debug("Skipping malformed cookie entry for domain '{0}': {1}", domain, ex.Message);
                }
            }

            return container;
        }

        private static bool TryParsePublishedAt(string value, out DateTime result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = default;
                return false;
            }

            if (DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out result))
            {
                result = result.ToUniversalTime();
                return true;
            }

            return false;
        }
    }
}
