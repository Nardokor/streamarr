#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;

namespace Streamarr.Core.MetadataSource.Fansly
{
    public class Fansly : MetadataSourceBase<FanslySettings>
    {
        private readonly IFanslyApiClient _client;
        private readonly Logger _logger;

        public Fansly(IFanslyApiClient client, Logger logger)
        {
            _client = client;
            _logger = logger;
        }

        public override string Name => "Fansly";
        public override PlatformType Platform => PlatformType.Fansly;

        // ── Validation ─────────────────────────────────────────────────────────

        public override ValidationResult Test()
        {
            if (string.IsNullOrWhiteSpace(Settings.AuthToken))
            {
                return new ValidationResult(new[]
                {
                    new ValidationFailure("AuthToken", "Auth token is required.")
                });
            }

            try
            {
                // Probe a known public account to verify the token is accepted
                _client.GetAccount(Settings.AuthToken, "fansly");
                return new ValidationResult();
            }
            catch (Exception ex)
            {
                return new ValidationResult(new[]
                {
                    new ValidationFailure("AuthToken", $"Could not connect to Fansly: {ex.Message}")
                });
            }
        }

        // ── Discovery ──────────────────────────────────────────────────────────

        public override CreatorMetadataResult SearchCreator(string query)
        {
            query = (query ?? string.Empty).Trim();
            var username = ExtractUsername(query);

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException(
                    "Please enter a Fansly URL (e.g. https://fansly.com/creatorname) or username.");
            }

            _logger.Info("Looking up Fansly account: {0}", username);

            var account = _client.GetAccount(Settings.AuthToken, username);
            if (account == null)
            {
                throw new InvalidOperationException(
                    $"No Fansly account found for '{username}'. Check the username and ensure your auth token is valid.");
            }

            return BuildCreatorResult(account);
        }

        public override ChannelMetadataResult GetChannelMetadata(string platformUrl)
        {
            var username = ExtractUsername(platformUrl);
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException($"Cannot derive Fansly username from URL: {platformUrl}");
            }

            var account = _client.GetAccount(Settings.AuthToken, username);
            if (account == null)
            {
                throw new InvalidOperationException(
                    $"Fansly account for '{username}' not found. Check your auth token.");
            }

            return BuildChannelResult(account);
        }

        // ── Content sync ───────────────────────────────────────────────────────

        public override IEnumerable<ContentMetadataResult> GetNewContent(
            string platformUrl,
            string platformId,
            DateTime? since,
            bool checkMembership = false)
        {
            _logger.Info("Fetching Fansly posts for account {0} (since: {1})", platformId, since?.ToString("u") ?? "all");

            var posts = _client.GetTimeline(Settings.AuthToken, platformId, since);

            var results = posts.Select(post => new ContentMetadataResult
            {
                PlatformContentId = post.Id,
                PlatformChannelId = platformId,
                PlatformChannelTitle = post.AccountId,
                ContentType = ContentType.Video,
                Title = ExtractTitle(post.Content, post.Id),
                Description = post.Content ?? string.Empty,
                ThumbnailUrl = string.Empty,
                AirDateUtc = DateTimeOffset.FromUnixTimeSeconds(post.CreatedAt).UtcDateTime,
                IsAccessible = true
            }).ToList();

            _logger.Info("GetNewContent for Fansly account {0}: {1} video post(s)", platformId, results.Count);
            return results;
        }

        // ── Single / batch lookup ──────────────────────────────────────────────

        public override ContentMetadataResult? GetContentMetadata(string platformContentId)
        {
            try
            {
                var payload = _client.GetPost(Settings.AuthToken, platformContentId);
                var post = payload?.Posts?.Count > 0 ? payload.Posts[0] : null;
                if (post == null)
                {
                    return null;
                }

                return new ContentMetadataResult
                {
                    PlatformContentId = post.Id,
                    PlatformChannelId = post.AccountId,
                    PlatformChannelTitle = string.Empty,
                    ContentType = ContentType.Video,
                    Title = ExtractTitle(post.Content, post.Id),
                    Description = post.Content ?? string.Empty,
                    ThumbnailUrl = string.Empty,
                    AirDateUtc = DateTimeOffset.FromUnixTimeSeconds(post.CreatedAt).UtcDateTime,
                    IsAccessible = true
                };
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to fetch Fansly post {0}", platformContentId);
                return null;
            }
        }

        public override IEnumerable<ContentMetadataResult> GetContentMetadataBatch(
            IEnumerable<string> platformContentIds)
        {
            var results = new List<ContentMetadataResult>();
            foreach (var id in platformContentIds)
            {
                var result = GetContentMetadata(id);
                if (result != null)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        // ── Livestream status ──────────────────────────────────────────────────

        public override IEnumerable<ContentStatusUpdate> GetLivestreamStatusUpdates(
            IEnumerable<string> platformContentIds)
        {
            // Fansly live stream tracking is not yet implemented.
            return Enumerable.Empty<ContentStatusUpdate>();
        }

        public override string GetDownloadUrl(string platformContentId) =>
            $"https://fansly.com/post/{platformContentId}";

        // ── Helpers ────────────────────────────────────────────────────────────

        private static CreatorMetadataResult BuildCreatorResult(FanslyAccount account)
        {
            var channel = BuildChannelResult(account);
            return new CreatorMetadataResult
            {
                Name = account.DisplayName ?? account.Username ?? account.Id,
                Description = account.About ?? string.Empty,
                ThumbnailUrl = string.Empty,
                Channels = new List<ChannelMetadataResult> { channel }
            };
        }

        private static ChannelMetadataResult BuildChannelResult(FanslyAccount account)
        {
            return new ChannelMetadataResult
            {
                Platform = PlatformType.Fansly,
                PlatformId = account.Id,
                PlatformUrl = $"https://fansly.com/{account.Username}",
                Title = account.DisplayName ?? account.Username ?? account.Id,
                Description = account.About ?? string.Empty,
                ThumbnailUrl = string.Empty
            };
        }

        // Uses the first line of the post content as the title, falling back to the post ID.
        private static string ExtractTitle(string? content, string postId)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return $"Fansly post {postId}";
            }

            var firstLine = content.Split('\n')[0].Trim();
            return string.IsNullOrWhiteSpace(firstLine) ? $"Fansly post {postId}" : firstLine;
        }

        // Extracts the username from a Fansly URL or returns the bare slug as-is.
        private static string? ExtractUsername(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(input);
                    if (uri.Host.IndexOf("fansly.com", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        return null;
                    }

                    var segments = uri.AbsolutePath.Trim('/').Split('/');
                    return segments.Length > 0 && !string.IsNullOrWhiteSpace(segments[0])
                        ? segments[0]
                        : null;
                }
                catch
                {
                    return null;
                }
            }

            return input.Contains(' ') ? null : input;
        }
    }
}
