using RestSharp;
using Streamarr.Api.V1.Queue;

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
