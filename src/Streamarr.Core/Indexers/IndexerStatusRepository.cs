using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.ThingiProvider.Status;

namespace Streamarr.Core.Indexers
{
    public interface IIndexerStatusRepository : IProviderStatusRepository<IndexerStatus>
    {
    }

    public class IndexerStatusRepository : ProviderStatusRepository<IndexerStatus>, IIndexerStatusRepository
    {
        public IndexerStatusRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
