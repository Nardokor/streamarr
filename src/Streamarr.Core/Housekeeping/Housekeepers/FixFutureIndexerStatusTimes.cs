using Streamarr.Core.Indexers;

namespace Streamarr.Core.Housekeeping.Housekeepers
{
    public class FixFutureIndexerStatusTimes : FixFutureProviderStatusTimes<IndexerStatus>, IHousekeepingTask
    {
        public FixFutureIndexerStatusTimes(IIndexerStatusRepository indexerStatusRepository)
            : base(indexerStatusRepository)
        {
        }
    }
}
