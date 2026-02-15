using Streamarr.Common.Extensions;
using Streamarr.Core.Configuration;
using Streamarr.Core.Localization;

namespace Streamarr.Core.HealthCheck.Checks
{
    public class PackageGlobalMessageCheck : HealthCheckBase
    {
        private readonly IDeploymentInfoProvider _deploymentInfoProvider;

        public PackageGlobalMessageCheck(IDeploymentInfoProvider deploymentInfoProvider, ILocalizationService localizationService)
            : base(localizationService)
        {
            _deploymentInfoProvider = deploymentInfoProvider;
        }

        public override HealthCheck Check()
        {
            if (_deploymentInfoProvider.PackageGlobalMessage.IsNullOrWhiteSpace())
            {
                return new HealthCheck(GetType());
            }

            var message = _deploymentInfoProvider.PackageGlobalMessage;
            var result = HealthCheckResult.Notice;

            if (message.StartsWith("Error:"))
            {
                message = message.Substring(6);
                result = HealthCheckResult.Error;
            }
            else if (message.StartsWith("Warn:"))
            {
                message = message.Substring(5);
                result = HealthCheckResult.Warning;
            }

            return new HealthCheck(GetType(),
                result,
                HealthCheckReason.Package,
                message,
                "#package-maintainer-message");
        }
    }
}
