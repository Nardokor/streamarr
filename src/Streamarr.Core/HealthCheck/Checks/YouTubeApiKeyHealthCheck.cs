using System;
using Streamarr.Core.Configuration;
using Streamarr.Core.Configuration.Events;
using Streamarr.Core.Localization;
using Streamarr.Core.MetadataSource.YouTube;

namespace Streamarr.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ConfigSavedEvent))]
    public class YouTubeApiKeyHealthCheck : HealthCheckBase
    {
        private readonly IConfigService _configService;
        private readonly IYouTubeApiClient _youTubeApiClient;

        public YouTubeApiKeyHealthCheck(IConfigService configService,
                                        IYouTubeApiClient youTubeApiClient,
                                        ILocalizationService localizationService)
            : base(localizationService)
        {
            _configService = configService;
            _youTubeApiClient = youTubeApiClient;
        }

        public override HealthCheck Check()
        {
            var apiKey = _configService.YouTubeApiKey;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new HealthCheck(
                    GetType(),
                    HealthCheckResult.Warning,
                    HealthCheckReason.YouTubeApiKeyNotConfigured,
                    "YouTube API key is not configured. Some metadata features may be unavailable.");
            }

            try
            {
                _youTubeApiClient.TestApiKey(apiKey);
            }
            catch (Exception ex)
            {
                return new HealthCheck(
                    GetType(),
                    HealthCheckResult.Error,
                    HealthCheckReason.YouTubeApiKeyInvalid,
                    $"YouTube API key is invalid or could not be verified: {ex.Message}");
            }

            return new HealthCheck(GetType());
        }
    }
}
