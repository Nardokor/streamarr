using Streamarr.Core.Parser.Model;
using Streamarr.Core.ThingiProvider.Status;

namespace Streamarr.Core.Indexers
{
    public class IndexerStatus : ProviderStatusBase
    {
        public ReleaseInfo LastRssSyncReleaseInfo { get; set; }
    }
}
