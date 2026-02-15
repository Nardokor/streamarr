using Streamarr.Core.Download;

namespace Streamarr.Core.Housekeeping.Housekeepers
{
    public class FixFutureDownloadClientStatusTimes : FixFutureProviderStatusTimes<DownloadClientStatus>, IHousekeepingTask
    {
        public FixFutureDownloadClientStatusTimes(IDownloadClientStatusRepository downloadClientStatusRepository)
            : base(downloadClientStatusRepository)
        {
        }
    }
}
