using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Profiles.Delay
{
    public interface IDelayProfileRepository : IBasicRepository<DelayProfile>
    {
    }

    public class DelayProfileRepository : BasicRepository<DelayProfile>, IDelayProfileRepository
    {
        public DelayProfileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
