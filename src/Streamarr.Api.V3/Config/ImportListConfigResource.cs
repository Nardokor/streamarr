using Streamarr.Core.Configuration;
using Streamarr.Core.ImportLists;
using Streamarr.Http.REST;

namespace Streamarr.Api.V3.Config
{
    public class ImportListConfigResource : RestResource
    {
        public ListSyncLevelType ListSyncLevel { get; set; }
        public int ListSyncTag { get; set; }
    }

    public static class ImportListConfigResourceMapper
    {
        public static ImportListConfigResource ToResource(IConfigService model)
        {
            return new ImportListConfigResource
            {
                ListSyncLevel = model.ListSyncLevel,
                ListSyncTag = model.ListSyncTag,
            };
        }
    }
}
