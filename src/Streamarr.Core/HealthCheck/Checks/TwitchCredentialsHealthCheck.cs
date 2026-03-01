using System.Linq;
using Streamarr.Core.Channels;
using Streamarr.Core.Localization;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.MetadataSource.Twitch;

namespace Streamarr.Core.HealthCheck.Checks
{
    public class TwitchCredentialsHealthCheck : HealthCheckBase
    {
        private readonly IMetadataSourceFactory _metadataSourceFactory;

        public TwitchCredentialsHealthCheck(IMetadataSourceFactory metadataSourceFactory,
                                            ILocalizationService localizationService)
            : base(localizationService)
        {
            _metadataSourceFactory = metadataSourceFactory;
        }

        public override HealthCheck Check()
        {
            var source = _metadataSourceFactory.GetByPlatform(PlatformType.Twitch);

            if (source == null)
            {
                return new HealthCheck(GetType());
            }

            var settings = source.Definition.Settings as TwitchSettings;

            if (string.IsNullOrWhiteSpace(settings?.ClientId) ||
                string.IsNullOrWhiteSpace(settings?.ClientSecret))
            {
                return new HealthCheck(
                    GetType(),
                    HealthCheckResult.Error,
                    HealthCheckReason.TwitchCredentialsMissing,
                    "Twitch source is enabled but Client ID or Client Secret is not configured.");
            }

            var result = source.Test();

            if (!result.IsValid)
            {
                var firstError = result.Errors.FirstOrDefault()?.ErrorMessage ?? "Unknown error";
                return new HealthCheck(
                    GetType(),
                    HealthCheckResult.Error,
                    HealthCheckReason.TwitchCredentialsInvalid,
                    $"Twitch credentials are invalid or could not be verified: {firstError}");
            }

            return new HealthCheck(GetType());
        }
    }
}
