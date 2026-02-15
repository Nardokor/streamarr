using Streamarr.Api.V5.Provider;
using Streamarr.Core.Notifications;

namespace Streamarr.Api.V5.Connections;

public class ConnectionBulkResource : ProviderBulkResource<ConnectionBulkResource>
{
}

public class ConnectionBulkResourceMapper : ProviderBulkResourceMapper<ConnectionBulkResource, NotificationDefinition>
{
}
