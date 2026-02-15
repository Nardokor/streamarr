using System.Linq;
using NLog;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Common.Instrumentation.Sentry;
using Streamarr.Core.Configuration;
using Streamarr.Core.Datastore;
using Streamarr.Core.Lifecycle;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Instrumentation
{
    public class ReconfigureSentry : IHandleAsync<ApplicationStartedEvent>
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IPlatformInfo _platformInfo;
        private readonly IMainDatabase _database;

        public ReconfigureSentry(IConfigFileProvider configFileProvider,
                                 IPlatformInfo platformInfo,
                                 IMainDatabase database)
        {
            _configFileProvider = configFileProvider;
            _platformInfo = platformInfo;
            _database = database;
        }

        public void Reconfigure()
        {
            // Extended sentry config
            var sentryTarget = LogManager.Configuration.AllTargets.OfType<SentryTarget>().FirstOrDefault();
            if (sentryTarget != null)
            {
                sentryTarget.UpdateScope(_database.Version, _database.Migration, _configFileProvider.Branch, _platformInfo);
            }
        }

        public void HandleAsync(ApplicationStartedEvent message)
        {
            Reconfigure();
        }
    }
}
