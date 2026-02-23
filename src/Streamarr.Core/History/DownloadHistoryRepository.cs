using System.Collections.Generic;
using System.Linq;
using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.History
{
    public interface IDownloadHistoryRepository : IBasicRepository<DownloadHistory>
    {
        List<DownloadHistory> GetByCreatorId(int creatorId);
        List<DownloadHistory> GetRecent(int count);
    }

    public class DownloadHistoryRepository : BasicRepository<DownloadHistory>, IDownloadHistoryRepository
    {
        public DownloadHistoryRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<DownloadHistory> GetByCreatorId(int creatorId)
        {
            return Query(h => h.CreatorId == creatorId);
        }

        public List<DownloadHistory> GetRecent(int count)
        {
            return All().ToList();
        }
    }
}
