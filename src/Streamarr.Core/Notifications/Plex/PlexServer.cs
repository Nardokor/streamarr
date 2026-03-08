using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation.Results;
using NLog;
using Streamarr.Core.Configuration;

namespace Streamarr.Core.Notifications.Plex
{
    public class PlexServer : NotificationBase<PlexServerSettings>
    {
        private static readonly HttpClient _http = new HttpClient();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public PlexServer(IConfigService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public override string Name => "Plex Media Server";

        public override void OnDownload(ContentDownloadedMessage message)
        {
            RefreshLibrary();
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "startOAuth")
            {
                return StartOAuth();
            }

            if (action == "getOAuthToken")
            {
                query.TryGetValue("pinId", out var pinId);
                return GetOAuthToken(pinId);
            }

            return null;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                var scheme = Settings.UseSsl ? "https" : "http";
                var url = $"{scheme}://{Settings.Host}:{Settings.Port}/?X-Plex-Token={Settings.AuthToken}";
                var response = _http.GetAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to connect to Plex server");
                failures.Add(new ValidationFailure("Host", $"Unable to connect to Plex server: {ex.Message}"));
            }

            return new ValidationResult(failures);
        }

        private object StartOAuth()
        {
            var clientId = _configService.PlexClientIdentifier;

            var request = new HttpRequestMessage(HttpMethod.Post, "https://plex.tv/api/v2/pins");
            AddPlexHeaders(request, clientId);
            request.Content = new StringContent("{\"strong\":true}", Encoding.UTF8, "application/json");

            var response = _http.SendAsync(request).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var pin = JsonSerializer.Deserialize<PlexPin>(json, _jsonOptions);

            var oAuthUrl = $"https://app.plex.tv/auth#?" +
                           $"clientID={Uri.EscapeDataString(clientId)}" +
                           $"&code={pin.Code}" +
                           $"&context%5Bdevice%5D%5Bproduct%5D=Streamarr";

            _logger.Debug("Plex OAuth PIN {0} created (code: {1})", pin.Id, pin.Code);

            return new { OAuthUrl = oAuthUrl, PinId = pin.Id };
        }

        private object GetOAuthToken(string pinId)
        {
            if (string.IsNullOrWhiteSpace(pinId))
            {
                return new { AuthToken = (string)null };
            }

            var clientId = _configService.PlexClientIdentifier;

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://plex.tv/api/v2/pins/{pinId}");
            AddPlexHeaders(request, clientId);

            var response = _http.SendAsync(request).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var pin = JsonSerializer.Deserialize<PlexPin>(json, _jsonOptions);

            return new { AuthToken = pin.AuthToken };
        }

        private void RefreshLibrary()
        {
            var scheme = Settings.UseSsl ? "https" : "http";
            var url = $"{scheme}://{Settings.Host}:{Settings.Port}/library/sections/all/refresh?X-Plex-Token={Settings.AuthToken}";
            var response = _http.GetAsync(url).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Warn("Plex library refresh returned status {0}", response.StatusCode);
            }
            else
            {
                _logger.Debug("Plex library refresh triggered successfully");
            }
        }

        private static void AddPlexHeaders(HttpRequestMessage request, string clientId)
        {
            request.Headers.Add("X-Plex-Client-Identifier", clientId);
            request.Headers.Add("X-Plex-Product", "Streamarr");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private class PlexPin
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("code")]
            public string Code { get; set; }

            [JsonPropertyName("authToken")]
            public string AuthToken { get; set; }
        }
    }
}
