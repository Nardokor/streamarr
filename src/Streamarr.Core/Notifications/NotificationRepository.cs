using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.Notifications
{
    public interface INotificationRepository : IProviderRepository<NotificationDefinition>
    {
        void UpdateSettings(NotificationDefinition model);
    }

    public class NotificationRepository : ProviderRepository<NotificationDefinition>, INotificationRepository
    {
        public NotificationRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public void UpdateSettings(NotificationDefinition model)
        {
            SetFields(model, m => m.Settings);
        }
    }
}
