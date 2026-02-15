using Microsoft.AspNetCore.Mvc;
using Streamarr.Api.V1.Provider;
using Streamarr.Core.Notifications;
using Streamarr.Http;
using Streamarr.SignalR;

namespace Streamarr.Api.V1.Connections;

[V1ApiController]
public class ConnectionController : ProviderControllerBase<ConnectionResource, ConnectionBulkResource, INotification, NotificationDefinition>
{
    public static readonly ConnectionResourceMapper ResourceMapper = new();
    public static readonly ConnectionBulkResourceMapper BulkResourceMapper = new();

    public ConnectionController(IBroadcastSignalRMessage signalRBroadcaster, NotificationFactory notificationFactory)
        : base(signalRBroadcaster, notificationFactory, "connection", ResourceMapper, BulkResourceMapper)
    {
    }

    [NonAction]
    public override ActionResult<ConnectionResource> UpdateProvider([FromBody] ConnectionBulkResource providerResource)
    {
        throw new NotImplementedException();
    }

    [NonAction]
    public override ActionResult DeleteProviders([FromBody] ConnectionBulkResource resource)
    {
        throw new NotImplementedException();
    }
}
