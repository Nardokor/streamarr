using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.ThingiProvider.Status;

namespace Streamarr.Core.ImportLists
{
    public interface IImportListStatusRepository : IProviderStatusRepository<ImportListStatus>
    {
    }

    public class ImportListStatusRepository : ProviderStatusRepository<ImportListStatus>, IImportListStatusRepository
    {
        public ImportListStatusRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
