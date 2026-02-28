using Streamarr.Core.Download.YtDlp;
using Streamarr.Core.Localization;

namespace Streamarr.Core.HealthCheck.Checks
{
    public class YtDlpHealthCheck : HealthCheckBase
    {
        private readonly IYtDlpClient _ytDlpClient;

        public YtDlpHealthCheck(IYtDlpClient ytDlpClient, ILocalizationService localizationService)
            : base(localizationService)
        {
            _ytDlpClient = ytDlpClient;
        }

        public override HealthCheck Check()
        {
            if (!_ytDlpClient.IsAvailable())
            {
                return new HealthCheck(
                    GetType(),
                    HealthCheckResult.Error,
                    HealthCheckReason.YtDlpNotAvailable,
                    "yt-dlp is not available. Install yt-dlp and ensure it is accessible via the configured binary path.");
            }

            return new HealthCheck(GetType());
        }
    }
}
