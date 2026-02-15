using System.Collections.Generic;
using RestSharp;
using Streamarr.Api.V3.Notifications;

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
