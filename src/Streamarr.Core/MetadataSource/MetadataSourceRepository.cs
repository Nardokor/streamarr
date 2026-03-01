using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.MetadataSource
{
    public class MetadataSourceRepository : ProviderRepository<MetadataSourceDefinition>, IProviderRepository<MetadataSourceDefinition>
    {
        public MetadataSourceRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
