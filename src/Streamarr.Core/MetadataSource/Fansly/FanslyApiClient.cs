using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using NLog;

namespace Streamarr.Core.MetadataSource.Fansly
{
    public interface IFanslyApiClient
    {
        FanslyAccount GetAccount(string authToken, string username);
        List<FanslyPost> GetTimeline(string authToken, string accountId, DateTime? since);
        FanslyTimelinePayload GetPost(string authToken, string postId);
    }

    public class FanslyApiClient : IFanslyApiClient
    {
        private const string ApiBase = "https://apiv3.fansly.com/api/v1";
        private const int PageSize = 10;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Logger _logger;

        public FanslyApiClient(Logger logger)
        {
            _logger = logger;
        }

        public FanslyAccount GetAccount(string authToken, string username)
        {
            var url = $"{ApiBase}/account?usernames={Uri.EscapeDataString(username)}&ngsw-bypass=true";
            var response = Fetch<FanslyAccountResponse>(authToken, url);
            return response?.Success == true && response.Response?.Count > 0
                ? response.Response[0]
                : null;
        }

        // Returns posts from the account's timeline, newest-first, stopping when
        // all remaining posts are at or before `since`.
        // Fansly does not return a reliable next-page cursor, so pagination uses
        // the last post's ID as the `before` parameter and stops when a short page
        // (fewer than PageSize results) is returned.
        public List<FanslyPost> GetTimeline(string authToken, string accountId, DateTime? since)
        {
            var results = new List<FanslyPost>();
            string cursor = null;

            while (true)
            {
                var url = $"{ApiBase}/timeline/{Uri.EscapeDataString(accountId)}?limit={PageSize}&ngsw-bypass=true";
                if (cursor != null)
                {
                    url += $"&before={Uri.EscapeDataString(cursor)}";
                }

                var response = Fetch<FanslyTimelineResponse>(authToken, url);
                var payload = response?.Response;

                if (payload?.Posts == null || payload.Posts.Count == 0)
                {
                    break;
                }

                // Build a lookup from accountMedia.Id → media type for this page.
                // type 2 = video, type 1 = image.
                var mediaTypeById = new Dictionary<string, int>();
                if (payload.AccountMedia != null)
                {
                    foreach (var am in payload.AccountMedia)
                    {
                        if (am.Id != null && am.Media != null)
                        {
                            mediaTypeById[am.Id] = am.Media.Type;
                        }
                    }
                }

                var doneEarly = false;
                foreach (var post in payload.Posts)
                {
                    var postDate = DateTimeOffset.FromUnixTimeSeconds(post.CreatedAt).UtcDateTime;
                    if (since.HasValue && postDate <= since.Value)
                    {
                        doneEarly = true;
                        break;
                    }

                    if (post.Attachments == null || post.Attachments.Count == 0)
                    {
                        continue;
                    }

                    // Include the post only if at least one attachment is a video (type 2),
                    // or if its contentId isn't in the sideload (unknown — include conservatively).
                    var hasVideo = false;
                    var allKnownNonVideo = true;
                    foreach (var attachment in post.Attachments)
                    {
                        if (attachment.ContentId == null)
                        {
                            continue;
                        }

                        if (mediaTypeById.TryGetValue(attachment.ContentId, out var mediaType))
                        {
                            if (mediaType == 2)
                            {
                                hasVideo = true;
                                break;
                            }
                        }
                        else
                        {
                            // Not in sideload — type unknown; don't treat as confirmed non-video.
                            allKnownNonVideo = false;
                        }
                    }

                    if (hasVideo || !allKnownNonVideo)
                    {
                        results.Add(post);
                    }
                }

                // Stop when the page is short (last page) or we hit the since cutoff.
                if (doneEarly || payload.Posts.Count < PageSize)
                {
                    break;
                }

                // Use the last post's ID as the cursor for the next page.
                cursor = payload.Posts[payload.Posts.Count - 1].Id;
            }

            _logger.Debug("Fansly GetTimeline for {0}: {1} video post(s) (since: {2})", accountId, results.Count, since?.ToString("u") ?? "all");
            return results;
        }

        public FanslyTimelinePayload GetPost(string authToken, string postId)
        {
            var url = $"{ApiBase}/post?ids={Uri.EscapeDataString(postId)}&ngsw-bypass=true";
            var response = Fetch<FanslyPostResponse>(authToken, url);
            return response?.Success == true ? response.Response : null;
        }

        private T Fetch<T>(string authToken, string url)
        {
            var handler = new HttpClientHandler
            {
                CheckCertificateRevocationList = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Authorization", authToken);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            using var response = client.GetAsync(url).GetAwaiter().GetResult();

            if ((int)response.StatusCode == 429)
            {
                // Fansly rate-limits rapid successive calls (e.g. Test then Save).
                // Wait the amount the server suggests, or default to 2 seconds, then retry once.
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                System.Threading.Thread.Sleep(retryAfter);

                using var retryResponse = client.GetAsync(url).GetAwaiter().GetResult();
                if (!retryResponse.IsSuccessStatusCode)
                {
                    var retryBody = retryResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    throw new InvalidOperationException(
                        $"Fansly API returned {(int)retryResponse.StatusCode} for {url}: {retryBody}");
                }

                using var retryStream = retryResponse.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                return JsonSerializer.Deserialize<T>(retryStream, _jsonOptions);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new InvalidOperationException(
                    $"Fansly API returned {(int)response.StatusCode} for {url}: {body}");
            }

            using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<T>(stream, _jsonOptions);
        }
    }
}
