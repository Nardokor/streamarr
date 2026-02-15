using System.Collections.Generic;
using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.ImportLists.ImportListItems
{
    public interface IImportListItemRepository : IBasicRepository<ImportListItemInfo>
    {
        List<ImportListItemInfo> GetAllForLists(List<int> listIds);
    }

    public class ImportListItemRepository : BasicRepository<ImportListItemInfo>, IImportListItemRepository
    {
        public ImportListItemRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<ImportListItemInfo> GetAllForLists(List<int> listIds)
        {
            return Query(x => listIds.Contains(x.ImportListId));
        }
    }
}
