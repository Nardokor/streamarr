using System;
using NLog;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.ThingiProvider.Status;

namespace Streamarr.Core.Notifications
{
    public interface INotificationStatusService : IProviderStatusServiceBase<NotificationStatus>
    {
    }

    public class NotificationStatusService : ProviderStatusServiceBase<INotification, NotificationStatus>, INotificationStatusService
    {
        public NotificationStatusService(INotificationStatusRepository providerStatusRepository, IEventAggregator eventAggregator, IRuntimeInfo runtimeInfo, Logger logger)
            : base(providerStatusRepository, eventAggregator, runtimeInfo, logger)
        {
            MinimumTimeSinceInitialFailure = TimeSpan.FromMinutes(5);
            MaximumEscalationLevel = 5;
        }
    }
}
