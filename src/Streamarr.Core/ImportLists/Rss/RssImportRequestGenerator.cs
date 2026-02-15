using System.Collections.Generic;
using Streamarr.Common.Http;

namespace Streamarr.Core.ImportLists.Rss
{
    public class RssImportRequestGenerator<TSettings> : IImportListRequestGenerator
        where TSettings : RssImportBaseSettings<TSettings>, new()
    {
        public RssImportBaseSettings<TSettings> Settings { get; set; }

        public virtual ImportListPageableRequestChain GetListItems()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            pageableRequests.Add(GetSeriesRequest());

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetSeriesRequest()
        {
            yield return new ImportListRequest(Settings.Url, HttpAccept.Rss);
        }
    }
}
