using System;
using System.Collections.Generic;
using NLog;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.Notifications
{
    public class NotificationFactory : ProviderFactory<INotification, NotificationDefinition>
    {
        public NotificationFactory(NotificationRepository notificationRepository,
                                   IEnumerable<INotification> notifications,
                                   IServiceProvider container,
                                   IEventAggregator eventAggregator,
                                   Logger logger)
            : base(notificationRepository, notifications, container, eventAggregator, logger)
        {
        }

        public override void SetProviderCharacteristics(INotification provider, NotificationDefinition definition)
        {
            base.SetProviderCharacteristics(provider, definition);
            definition.SupportsOnDownload = provider.SupportsOnDownload;
        }
    }
}
