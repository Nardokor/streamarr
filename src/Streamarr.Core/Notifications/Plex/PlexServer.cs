using System.Collections.Generic;
using System.Net.Http;
using FluentValidation.Results;
using NLog;

namespace Streamarr.Core.Notifications.Plex
{
    public class PlexServer : NotificationBase<PlexServerSettings>
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly Logger _logger;

        public PlexServer(Logger logger)
        {
            _logger = logger;
        }

        public override string Name => "Plex Media Server";

        public override void OnDownload(ContentDownloadedMessage message)
        {
            RefreshLibrary();
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
            catch (System.Exception ex)
            {
                _logger.Error(ex, "Unable to connect to Plex server");
                failures.Add(new ValidationFailure("Host", $"Unable to connect to Plex server: {ex.Message}"));
            }

            return new ValidationResult(failures);
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
    }
}
