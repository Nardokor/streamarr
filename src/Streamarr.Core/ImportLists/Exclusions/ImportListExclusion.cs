using Streamarr.Core.Datastore;

namespace Streamarr.Core.ImportLists.Exclusions
{
    public class ImportListExclusion : ModelBase
    {
        public int TvdbId { get; set; }
        public string Title { get; set; }
    }
}
