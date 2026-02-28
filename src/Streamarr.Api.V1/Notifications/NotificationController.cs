using Streamarr.Api.V1.Provider;
using Streamarr.Core.Notifications;
using Streamarr.Http;
using Streamarr.SignalR;

namespace Streamarr.Api.V1.Notifications
{
    [V1ApiController]
    public class NotificationController : ProviderControllerBase<NotificationResource, NotificationBulkResource, INotification, NotificationDefinition>
    {
        public NotificationController(NotificationFactory notificationFactory,
                                      IBroadcastSignalRMessage signalRBroadcaster)
            : base(signalRBroadcaster,
                   notificationFactory,
                   "notification",
                   new NotificationResourceMapper(),
                   new NotificationBulkResourceMapper())
        {
        }
    }
}
