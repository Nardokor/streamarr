using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation.Results;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.MetadataSource.Twitch
{
    public class Twitch : MetadataSourceBase<TwitchSettings>
    {
        // PlatformContentId prefix for in-progress live streams
        private const string LivePrefix = "live:";

        // Matches https://www.twitch.tv/username or https://twitch.tv/username
        private static readonly Regex TwitchUrlRegex = new Regex(
            @"^https?://(www\.)?twitch\.tv/([a-zA-Z0-9_]{1,25})/?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ITwitchApiClient _twitchApiClient;
        private readonly Logger _logger;

        private string _cachedToken;
        private DateTime _tokenExpiresAt = DateTime.MinValue;

        public Twitch(ITwitchApiClient twitchApiClient, Logger logger)
        {
            _twitchApiClient = twitchApiClient;
            _logger = logger;
        }

        public override string Name => "Twitch";
        public override PlatformType Platform => PlatformType.Twitch;

        public override IEnumerable<ProviderDefinition> DefaultDefinitions =>
            new List<ProviderDefinition>
            {
                new MetadataSourceDefinition
                {
                    Name = "Twitch",
                    Implementation = nameof(Twitch),
                    ConfigContract = nameof(TwitchSettings),
                    Platform = PlatformType.Twitch,
                    Enable = true,
                    Settings = new TwitchSettings()
                }
            };

        // ── Validation ─────────────────────────────────────────────────────────

        public override ValidationResult Test()
        {
            if (string.IsNullOrWhiteSpace(Settings.ClientId) ||
                string.IsNullOrWhiteSpace(Settings.ClientSecret))
            {
                return new ValidationResult(new[]
                {
                    new ValidationFailure("ClientId", "Twitch Client ID and Client Secret are both required.")
                });
            }

            try
            {
                InvalidateToken();
                _twitchApiClient.TestCredentials(Settings.ClientId, Settings.ClientSecret);
            }
            catch (Exception ex)
            {
                return new ValidationResult(new[]
                {
                    new ValidationFailure("ClientSecret", $"Twitch credential test failed: {ex.Message}")
                });
            }

            return new ValidationResult();
        }

        // ── Token management ───────────────────────────────────────────────────

        private string GetToken()
        {
            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiresAt)
            {
                return _cachedToken;
            }

            _logger.Debug("Fetching new Twitch access token");
            _cachedToken = _twitchApiClient.GetAccessToken(Settings.ClientId, Settings.ClientSecret);
            _tokenExpiresAt = DateTime.UtcNow.AddHours(23);
            return _cachedToken;
        }

        private void InvalidateToken()
        {
            _cachedToken = null;
            _tokenExpiresAt = DateTime.MinValue;
        }

        // Wraps an API call. On 401 (token expired), invalidates cache and retries once.
        private T CallApi<T>(Func<string, T> call)
        {
            try
            {
                return call(GetToken());
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("401"))
            {
                _logger.Warn("Twitch API returned 401 — refreshing access token and retrying");
                InvalidateToken();
                return call(GetToken());
            }
        }

        // ── Discovery ──────────────────────────────────────────────────────────

        public override CreatorMetadataResult SearchCreator(string query)
        {
            query = query?.Trim() ?? string.Empty;

            TwitchUser user;
            var urlMatch = TwitchUrlRegex.Match(query);

            if (urlMatch.Success)
            {
                var login = urlMatch.Groups[2].Value.ToLowerInvariant();
                _logger.Info("Twitch lookup by URL, login: {0}", login);
                user = CallApi(token => _twitchApiClient.GetUserByLogin(Settings.ClientId, token, login));

                if (user == null)
                {
                    throw new InvalidOperationException(
                        $"No Twitch user found for URL: {query}");
                }
            }
            else
            {
                _logger.Info("Twitch channel search: {0}", query);
                var channels = CallApi(token =>
                    _twitchApiClient.SearchChannels(Settings.ClientId, token, query));

                var best = channels.FirstOrDefault();
                if (best == null)
                {
                    throw new InvalidOperationException(
                        $"No Twitch channels found matching: {query}");
                }

                user = CallApi(token =>
                    _twitchApiClient.GetUserByLogin(Settings.ClientId, token, best.BroadcasterLogin));

                if (user == null)
                {
                    throw new InvalidOperationException(
                        $"Could not resolve Twitch user for login: {best.BroadcasterLogin}");
                }
            }

            return BuildCreatorResult(user);
        }

        public override ChannelMetadataResult GetChannelMetadata(string platformUrl)
        {
            var urlMatch = TwitchUrlRegex.Match((platformUrl ?? string.Empty).Trim());
            if (!urlMatch.Success)
            {
                throw new InvalidOperationException(
                    $"'{platformUrl}' is not a valid Twitch channel URL. " +
                    "Expected format: https://www.twitch.tv/username");
            }

            var login = urlMatch.Groups[2].Value.ToLowerInvariant();
            var user = CallApi(token => _twitchApiClient.GetUserByLogin(Settings.ClientId, token, login));

            if (user == null)
            {
                throw new InvalidOperationException(
                    $"Twitch user '{login}' not found.");
            }

            var channelInfo = CallApi(token =>
                _twitchApiClient.GetChannelInfo(Settings.ClientId, token, user.Id));

            return new ChannelMetadataResult
            {
                Platform = PlatformType.Twitch,
                PlatformId = user.Id,
                PlatformUrl = $"https://www.twitch.tv/{user.Login}",
                Title = user.DisplayName,
                Description = user.Description ?? string.Empty,
                ThumbnailUrl = user.ProfileImageUrl ?? string.Empty,
                Category = channelInfo?.GameName ?? string.Empty
            };
        }

        // ── Content sync ───────────────────────────────────────────────────────

        public override IEnumerable<ContentMetadataResult> GetNewContent(
            string platformUrl,
            string platformId,
            DateTime? since,
            bool checkMembership = false)
        {
            if (string.IsNullOrWhiteSpace(platformId))
            {
                throw new InvalidOperationException(
                    "Twitch GetNewContent requires platformId (numeric user ID). " +
                    $"Got: '{platformId}'");
            }

            _logger.Info("Fetching Twitch VODs for user {0} (since: {1})", platformId, since?.ToString("u") ?? "all");

            var results = new List<ContentMetadataResult>();

            // 1. Archived VODs
            var videos = CallApi(token =>
                _twitchApiClient.GetVideos(Settings.ClientId, token, platformId, since));

            foreach (var video in videos)
            {
                results.Add(MapVodToContent(video));
            }

            // 2. Clips (Stories)
            var clips = CallApi(token =>
                _twitchApiClient.GetClips(Settings.ClientId, token, platformId, since));

            foreach (var clip in clips)
            {
                results.Add(MapClipToContent(clip));
            }

            // 3. Live stream — look up login from user ID to call /streams
            var user = CallApi(token =>
                _twitchApiClient.GetUserById(Settings.ClientId, token, platformId));

            if (user != null)
            {
                var stream = CallApi(token =>
                    _twitchApiClient.GetLiveStream(Settings.ClientId, token, user.Login));

                if (stream != null)
                {
                    _logger.Info("User {0} is currently live — including live content item", user.Login);
                    results.Add(MapStreamToContent(stream));
                }
            }

            _logger.Info("GetNewContent for Twitch user {0}: {1} item(s)", platformId, results.Count);
            return results;
        }

        // ── Single / batch lookup ──────────────────────────────────────────────

        public override ContentMetadataResult GetContentMetadata(string platformContentId)
        {
            if ((platformContentId ?? string.Empty).StartsWith(LivePrefix))
            {
                // Live sentinel — no VOD record exists yet
                return null;
            }

            var video = CallApi(token =>
                _twitchApiClient.GetVideo(Settings.ClientId, token, platformContentId));

            return video == null ? null : MapVodToContent(video);
        }

        public override IEnumerable<ContentMetadataResult> GetContentMetadataBatch(
            IEnumerable<string> platformContentIds)
        {
            var results = new List<ContentMetadataResult>();
            foreach (var id in platformContentIds ?? Enumerable.Empty<string>())
            {
                if ((id ?? string.Empty).StartsWith(LivePrefix))
                {
                    continue;
                }

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
            var updates = new List<ContentStatusUpdate>();

            foreach (var id in platformContentIds ?? Enumerable.Empty<string>())
            {
                if (!(id ?? string.Empty).StartsWith(LivePrefix))
                {
                    continue;
                }

                var userLogin = id.Substring(LivePrefix.Length);

                TwitchStream stream;
                try
                {
                    stream = CallApi(token =>
                        _twitchApiClient.GetLiveStream(Settings.ClientId, token, userLogin));
                }
                catch (Exception ex)
                {
                    _logger.Warn("Could not check Twitch live status for '{0}': {1}", userLogin, ex.Message);
                    continue;
                }

                if (stream == null)
                {
                    // Stream ended. The archived VOD will appear in the next scheduled sync
                    // under its own real video ID.
                    updates.Add(new ContentStatusUpdate
                    {
                        PlatformContentId = id,
                        NewContentType = ContentType.Vod,
                        ExistsOnPlatform = false,
                        ShouldTriggerDownload = false
                    });
                }
                else
                {
                    DateTime? startedAt = null;
                    if (DateTime.TryParse(stream.StartedAt, null, DateTimeStyles.RoundtripKind, out var parsed))
                    {
                        startedAt = parsed.ToUniversalTime();
                    }

                    updates.Add(new ContentStatusUpdate
                    {
                        PlatformContentId = id,
                        NewContentType = ContentType.Live,
                        NewAirDateUtc = startedAt,
                        ExistsOnPlatform = true,
                        ShouldTriggerDownload = false
                    });
                }
            }

            return updates;
        }

        // ── Mapping helpers ────────────────────────────────────────────────────

        private static ContentMetadataResult MapVodToContent(TwitchVideo video)
        {
            DateTime? createdAt = null;
            if (DateTime.TryParse(video.CreatedAt, null, DateTimeStyles.RoundtripKind, out var parsed))
            {
                createdAt = parsed.ToUniversalTime();
            }

            // archive = completed livestream VOD; highlight/upload = regular video content
            var contentType = string.Equals(video.VideoType, "archive", StringComparison.OrdinalIgnoreCase)
                ? ContentType.Vod
                : ContentType.Video;

            return new ContentMetadataResult
            {
                PlatformContentId = video.Id,
                PlatformChannelId = video.UserId ?? string.Empty,
                PlatformChannelTitle = video.UserName ?? video.UserLogin ?? string.Empty,
                ContentType = contentType,
                Title = video.Title ?? string.Empty,
                Description = video.Description ?? string.Empty,
                ThumbnailUrl = TwitchApiClient.NormalizeThumbnailUrl(video.ThumbnailUrl ?? string.Empty),
                Duration = TwitchApiClient.ParseTwitchDuration(video.Duration),
                AirDateUtc = createdAt
            };
        }

        private static ContentMetadataResult MapClipToContent(TwitchClip clip)
        {
            DateTime? createdAt = null;
            if (DateTime.TryParse(clip.CreatedAt, null, DateTimeStyles.RoundtripKind, out var parsed))
            {
                createdAt = parsed.ToUniversalTime();
            }

            // Use the clip URL as PlatformContentId — yt-dlp downloads clips by URL,
            // not by clip ID.
            return new ContentMetadataResult
            {
                PlatformContentId = clip.Url ?? clip.Id,
                PlatformChannelId = clip.BroadcasterId ?? string.Empty,
                PlatformChannelTitle = clip.BroadcasterName ?? string.Empty,
                ContentType = ContentType.Short,
                Title = clip.Title ?? string.Empty,
                Description = string.Empty,
                ThumbnailUrl = clip.ThumbnailUrl ?? string.Empty,
                Duration = TimeSpan.FromSeconds(clip.Duration),
                AirDateUtc = createdAt
            };
        }

        private static ContentMetadataResult MapStreamToContent(TwitchStream stream)
        {
            DateTime? startedAt = null;
            if (DateTime.TryParse(stream.StartedAt, null, DateTimeStyles.RoundtripKind, out var parsed))
            {
                startedAt = parsed.ToUniversalTime();
            }

            return new ContentMetadataResult
            {
                PlatformContentId = $"{LivePrefix}{stream.UserLogin}",
                PlatformChannelId = stream.UserId ?? string.Empty,
                PlatformChannelTitle = stream.UserName ?? stream.UserLogin ?? string.Empty,
                ContentType = ContentType.Live,
                Title = stream.Title ?? string.Empty,
                Description = string.Empty,
                ThumbnailUrl = TwitchApiClient.NormalizeThumbnailUrl(stream.ThumbnailUrl ?? string.Empty),
                Duration = null,
                AirDateUtc = startedAt
            };
        }

        private static CreatorMetadataResult BuildCreatorResult(TwitchUser user)
        {
            var channel = new ChannelMetadataResult
            {
                Platform = PlatformType.Twitch,
                PlatformId = user.Id,
                PlatformUrl = $"https://www.twitch.tv/{user.Login}",
                Title = user.DisplayName,
                Description = user.Description ?? string.Empty,
                ThumbnailUrl = user.ProfileImageUrl ?? string.Empty
            };

            return new CreatorMetadataResult
            {
                Name = user.DisplayName,
                Description = user.Description ?? string.Empty,
                ThumbnailUrl = user.ProfileImageUrl ?? string.Empty,
                Channels = new List<ChannelMetadataResult> { channel }
            };
        }
    }
}
