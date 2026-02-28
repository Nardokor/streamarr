using Streamarr.Api.V1.Provider;

namespace Streamarr.Api.V1.Notifications
{
    public class NotificationBulkResource : ProviderBulkResource<NotificationBulkResource>
    {
    }

    public class NotificationBulkResourceMapper : ProviderBulkResourceMapper<NotificationBulkResource, Core.Notifications.NotificationDefinition>
    {
    }
}
