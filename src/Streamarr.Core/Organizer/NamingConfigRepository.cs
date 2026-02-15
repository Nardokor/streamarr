using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Organizer
{
    public interface INamingConfigRepository : IBasicRepository<NamingConfig>
    {
    }

    public class NamingConfigRepository : BasicRepository<NamingConfig>, INamingConfigRepository
    {
        public NamingConfigRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
