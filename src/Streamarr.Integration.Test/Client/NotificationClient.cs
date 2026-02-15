using System.Collections.Generic;
using RestSharp;
using Streamarr.Api.V1.Notifications;

namespace Streamarr.Integration.Test.Client
{
    public class NotificationClient : ClientBase<NotificationResource>
    {
        public NotificationClient(IRestClient restClient, string apiKey)
            : base(restClient, apiKey)
        {
        }

        public List<NotificationResource> Schema()
        {
            var request = BuildRequest("/schema");
            return Get<List<NotificationResource>>(request);
        }
    }
}
