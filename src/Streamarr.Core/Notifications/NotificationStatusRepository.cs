using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.ThingiProvider.Status;

namespace Streamarr.Core.Notifications
{
    public interface INotificationStatusRepository : IProviderStatusRepository<NotificationStatus>
    {
    }

    public class NotificationStatusRepository : ProviderStatusRepository<NotificationStatus>, INotificationStatusRepository
    {
        public NotificationStatusRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
