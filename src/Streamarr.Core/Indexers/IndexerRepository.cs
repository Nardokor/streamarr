using System.Linq;
using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.Indexers
{
    public interface IIndexerRepository : IProviderRepository<IndexerDefinition>
    {
        IndexerDefinition FindByName(string name);
    }

    public class IndexerRepository : ProviderRepository<IndexerDefinition>, IIndexerRepository
    {
        public IndexerRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public IndexerDefinition FindByName(string name)
        {
            return Query(i => i.Name == name).SingleOrDefault();
        }
    }
}
