using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.Download
{
    public interface IDownloadClientRepository : IProviderRepository<DownloadClientDefinition>
    {
    }

    public class DownloadClientRepository : ProviderRepository<DownloadClientDefinition>, IDownloadClientRepository
    {
        public DownloadClientRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
