using Streamarr.Core.Notifications;

namespace Streamarr.Core.Housekeeping.Housekeepers
{
    public class FixFutureNotificationStatusTimes : FixFutureProviderStatusTimes<NotificationStatus>, IHousekeepingTask
    {
        public FixFutureNotificationStatusTimes(INotificationStatusRepository notificationStatusRepository)
            : base(notificationStatusRepository)
        {
        }
    }
}
