using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.CustomFilters
{
    public interface ICustomFilterRepository : IBasicRepository<CustomFilter>
    {
    }

    public class CustomFilterRepository : BasicRepository<CustomFilter>, ICustomFilterRepository
    {
        public CustomFilterRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
