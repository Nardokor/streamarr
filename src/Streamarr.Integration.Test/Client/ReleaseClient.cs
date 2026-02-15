using RestSharp;
using Streamarr.Api.V1.Indexers;

namespace Streamarr.Integration.Test.Client
{
    public class ReleaseClient : ClientBase<ReleaseResource>
    {
        public ReleaseClient(IRestClient restClient, string apiKey)
            : base(restClient, apiKey)
        {
        }
    }
}
