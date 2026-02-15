using System.Collections.Generic;
using RestSharp;
using Streamarr.Api.V1.DownloadClient;

namespace Streamarr.Integration.Test.Client
{
    public class DownloadClientClient : ClientBase<DownloadClientResource>
    {
        public DownloadClientClient(IRestClient restClient, string apiKey)
            : base(restClient, apiKey)
        {
        }

        public List<DownloadClientResource> Schema()
        {
            var request = BuildRequest("/schema");
            return Get<List<DownloadClientResource>>(request);
        }
    }
}
