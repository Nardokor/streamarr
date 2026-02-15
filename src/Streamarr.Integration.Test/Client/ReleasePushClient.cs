using RestSharp;
using Streamarr.Api.V1.Indexers;

namespace Streamarr.Integration.Test.Client
{
    public class ReleasePushClient : ClientBase<ReleaseResource>
    {
        public ReleasePushClient(IRestClient restClient, string apiKey)
            : base(restClient, apiKey, "release/push")
        {
        }
    }
}
