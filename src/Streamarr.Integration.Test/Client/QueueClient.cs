using RestSharp;
using Streamarr.Api.V3.Queue;

namespace Streamarr.Integration.Test.Client
{
    public class QueueClient : ClientBase<QueueResource>
    {
        public QueueClient(IRestClient restClient, string apiKey)
            : base(restClient, apiKey)
        {
        }
    }
}
