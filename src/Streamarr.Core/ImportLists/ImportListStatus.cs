using System;
using Streamarr.Core.ThingiProvider.Status;

namespace Streamarr.Core.ImportLists
{
    public class ImportListStatus : ProviderStatusBase
    {
        public DateTime? LastInfoSync { get; set; }
        public bool HasRemovedItemSinceLastClean { get; set; }
    }
}
