using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.AutoTagging
{
    public interface IAutoTaggingRepository : IBasicRepository<AutoTag>
    {
    }

    public class AutoTaggingRepository : BasicRepository<AutoTag>, IAutoTaggingRepository
    {
        public AutoTaggingRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
