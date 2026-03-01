using System.Linq;
using Streamarr.Core.Channels;
using Streamarr.Core.Localization;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.MetadataSource.YouTube;

namespace Streamarr.Core.HealthCheck.Checks
{
    public class YouTubeApiKeyHealthCheck : HealthCheckBase
    {
        private readonly MetadataSourceFactory _metadataSourceFactory;

        public YouTubeApiKeyHealthCheck(MetadataSourceFactory metadataSourceFactory,
                                        ILocalizationService localizationService)
            : base(localizationService)
        {
            _metadataSourceFactory = metadataSourceFactory;
        }

        public override HealthCheck Check()
        {
            var source = _metadataSourceFactory.GetByPlatform(PlatformType.YouTube);

            if (source == null)
            {
                return new HealthCheck(GetType());
            }

            var settings = source.Definition.Settings as YouTubeSettings;
            var apiKey = settings?.ApiKey ?? string.Empty;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new HealthCheck(
                    GetType(),
                    HealthCheckResult.Warning,
                    HealthCheckReason.YouTubeApiKeyNotConfigured,
                    "YouTube API key is not configured. Some metadata features may be unavailable.");
            }

            var result = source.Test();

            if (!result.IsValid)
            {
                var firstError = result.Errors.FirstOrDefault()?.ErrorMessage ?? "Unknown error";
                return new HealthCheck(
                    GetType(),
                    HealthCheckResult.Error,
                    HealthCheckReason.YouTubeApiKeyInvalid,
                    $"YouTube API key is invalid or could not be verified: {firstError}");
            }

            return new HealthCheck(GetType());
        }
    }
}
