using Streamarr.Api.V1.Provider;
using Streamarr.Core.Notifications;

namespace Streamarr.Api.V1.Notifications
{
    public class NotificationResource : ProviderResource<NotificationResource>
    {
        public bool Enable { get; set; }
        public bool OnDownload { get; set; }
        public bool SupportsOnDownload { get; set; }
    }

    public class NotificationResourceMapper : ProviderResourceMapper<NotificationResource, NotificationDefinition>
    {
        public override NotificationResource ToResource(NotificationDefinition definition)
        {
            var resource = base.ToResource(definition);
            resource.Enable = definition.Enable;
            resource.OnDownload = definition.OnDownload;
            resource.SupportsOnDownload = definition.SupportsOnDownload;
            return resource;
        }

        public override NotificationDefinition ToModel(NotificationResource resource, NotificationDefinition? existingDefinition)
        {
            var definition = base.ToModel(resource, existingDefinition);
            definition.Enable = resource.Enable;
            definition.OnDownload = resource.OnDownload;
            return definition;
        }
    }
}
