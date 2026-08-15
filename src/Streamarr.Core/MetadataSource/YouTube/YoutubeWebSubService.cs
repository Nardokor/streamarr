using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using NLog;
using Streamarr.Core.Channels;

namespace Streamarr.Core.MetadataSource.YouTube
{
    public interface IYoutubeWebSubService
    {
        void SubscribeAll();
        bool VerifySignature(string secret, byte[] body, string signatureHeader);

        // Returns the hostname from the configured WebhookBaseUrl, or null if not set.
        // Used by middleware to identify requests arriving via the public Funnel URL.
        string GetWebhookHost();
    }

    public class YoutubeWebSubService : IYoutubeWebSubService
    {
        private const string HubUrl = "https://pubsubhubbub.appspot.com/subscribe";
        private const string TopicBase = "https://www.youtube.com/xml/feeds/videos.xml?channel_id=";
        private const int LeaseDays = 5;

        private static readonly HttpClient _http = new HttpClient();

        private readonly IChannelService _channelService;
        private readonly MetadataSourceFactory _metadataSourceFactory;
        private readonly Logger _logger;

        public YoutubeWebSubService(IChannelService channelService,
                                    MetadataSourceFactory metadataSourceFactory,
                                    Logger logger)
        {
            _channelService = channelService;
            _metadataSourceFactory = metadataSourceFactory;
            _logger = logger;
        }

        public void SubscribeAll()
        {
            var settings = GetYouTubeSettings();

            if (settings == null || string.IsNullOrWhiteSpace(settings.WebhookBaseUrl))
            {
                _logger.Debug("WebSub disabled — WebhookBaseUrl not configured in YouTube settings");
                return;
            }

            var callbackUrl = settings.WebhookBaseUrl.TrimEnd('/') + "/api/v1/webhook/youtube";

            var channels = _channelService.GetAllChannels()
                                          .Where(c => c.Platform == PlatformType.YouTube && c.Monitored)
                                          .ToList();

            _logger.Info("Renewing WebSub subscriptions for {0} YouTube channel(s)", channels.Count);

            foreach (var channel in channels)
            {
                Subscribe(channel, callbackUrl);
            }
        }

        public bool VerifySignature(string secret, byte[] body, string signatureHeader)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signatureHeader))
            {
                return false;
            }

            if (!signatureHeader.StartsWith("sha1=", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var expectedHex = signatureHeader.Substring(5);

            byte[] expectedBytes;
            try
            {
                expectedBytes = Convert.FromHexString(expectedHex);
            }
            catch (FormatException)
            {
                return false;
            }

            var secretBytes = System.Text.Encoding.UTF8.GetBytes(secret);

            using var hmac = new HMACSHA1(secretBytes);
            var computedBytes = hmac.ComputeHash(body);

            return CryptographicOperations.FixedTimeEquals(computedBytes, expectedBytes);
        }

        private void Subscribe(Channel channel, string callbackUrl)
        {
            if (string.IsNullOrEmpty(channel.WebSubSecret))
            {
                channel.WebSubSecret = GenerateSecret();
                _channelService.UpdateChannel(channel);
            }

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["hub.callback"] = callbackUrl,
                ["hub.topic"] = TopicBase + channel.PlatformId,
                ["hub.mode"] = "subscribe",
                ["hub.lease_seconds"] = (LeaseDays * 24 * 60 * 60).ToString(),
                ["hub.secret"] = channel.WebSubSecret
            });

            try
            {
                var response = _http.PostAsync(HubUrl, content).GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Warn(
                        "WebSub subscription request failed for channel {0} ({1}): HTTP {2}",
                        channel.Title,
                        channel.PlatformId,
                        (int)response.StatusCode);
                }
                else
                {
                    _logger.Debug(
                        "WebSub subscription accepted for channel {0} ({1})",
                        channel.Title,
                        channel.PlatformId);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(
                    ex,
                    "WebSub subscription request threw for channel {0} ({1})",
                    channel.Title,
                    channel.PlatformId);
            }
        }

        public string GetWebhookHost()
        {
            var settings = GetYouTubeSettings();

            if (settings == null || string.IsNullOrWhiteSpace(settings.WebhookBaseUrl))
            {
                return null;
            }

            if (Uri.TryCreate(settings.WebhookBaseUrl, UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }

            return null;
        }

        private static string GenerateSecret()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        private YouTubeSettings GetYouTubeSettings()
        {
            var def = _metadataSourceFactory.All()
                                            .FirstOrDefault(d => d.Enable && d.Settings is YouTubeSettings);

            return def?.Settings as YouTubeSettings;
        }
    }
}
