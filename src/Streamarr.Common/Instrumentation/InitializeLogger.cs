using System.Linq;
using NLog;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Common.Instrumentation.Sentry;

namespace Streamarr.Common.Instrumentation
{
    public class InitializeLogger
    {
        private readonly IOsInfo _osInfo;

        public InitializeLogger(IOsInfo osInfo)
        {
            _osInfo = osInfo;
        }

        public void Initialize()
        {
            var sentryTarget = LogManager.Configuration.AllTargets.OfType<SentryTarget>().FirstOrDefault();
            if (sentryTarget != null)
            {
                sentryTarget.UpdateScope(_osInfo);
            }
        }
    }
}
