using Streamarr.Api.V1.Provider;
using Streamarr.Core.Notifications;

namespace Streamarr.Api.V1.Connections;

public class ConnectionBulkResource : ProviderBulkResource<ConnectionBulkResource>
{
}

public class ConnectionBulkResourceMapper : ProviderBulkResourceMapper<ConnectionBulkResource, NotificationDefinition>
{
}
