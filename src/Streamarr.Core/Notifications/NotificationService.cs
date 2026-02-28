using NLog;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Notifications
{
    public class NotificationService : IHandle<ContentDownloadedEvent>
    {
        private readonly NotificationFactory _notificationFactory;
        private readonly Logger _logger;

        public NotificationService(NotificationFactory notificationFactory, Logger logger)
        {
            _notificationFactory = notificationFactory;
            _logger = logger;
        }

        public void Handle(ContentDownloadedEvent message)
        {
            var definitions = _notificationFactory.All();

            foreach (var definition in definitions)
            {
                if (!definition.Enable || !definition.OnDownload)
                {
                    continue;
                }

                var provider = _notificationFactory.GetInstance(definition);

                if (!provider.SupportsOnDownload)
                {
                    continue;
                }

                try
                {
                    provider.OnDownload(message.Message);
                }
                catch (System.Exception ex)
                {
                    _logger.Error(ex, "Error sending OnDownload notification to {0}", definition.Name);
                }
            }
        }
    }
}
