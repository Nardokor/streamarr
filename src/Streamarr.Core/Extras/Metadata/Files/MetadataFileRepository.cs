using Streamarr.Core.Datastore;
using Streamarr.Core.Extras.Files;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Extras.Metadata.Files
{
    public interface IMetadataFileRepository : IExtraFileRepository<MetadataFile>
    {
    }

    public class MetadataFileRepository : ExtraFileRepository<MetadataFile>, IMetadataFileRepository
    {
        public MetadataFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
