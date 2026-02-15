using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.ThingiProvider.Status;

namespace Streamarr.Core.Download
{
    public interface IDownloadClientStatusRepository : IProviderStatusRepository<DownloadClientStatus>
    {
    }

    public class DownloadClientStatusRepository : ProviderStatusRepository<DownloadClientStatus>, IDownloadClientStatusRepository
    {
        public DownloadClientStatusRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
