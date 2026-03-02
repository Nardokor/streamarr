using Streamarr.Api.V1.Provider;
using Streamarr.Core.Notifications;

namespace Streamarr.Api.V1.Notifications
{
    public class NotificationResource : ProviderResource<NotificationResource>
    {
        public bool Enable { get; set; }

        public bool OnGrab { get; set; }
        public bool SupportsOnGrab { get; set; }

        public bool OnDownload { get; set; }
        public bool SupportsOnDownload { get; set; }

        public bool OnLiveStreamStart { get; set; }
        public bool SupportsOnLiveStreamStart { get; set; }

        public bool OnLiveStreamEnd { get; set; }
        public bool SupportsOnLiveStreamEnd { get; set; }

        public bool OnChannelAdded { get; set; }
        public bool SupportsOnChannelAdded { get; set; }
    }

    public class NotificationResourceMapper : ProviderResourceMapper<NotificationResource, NotificationDefinition>
    {
        public override NotificationResource ToResource(NotificationDefinition definition)
        {
            var resource = base.ToResource(definition);
            resource.Enable = definition.Enable;
            resource.OnGrab = definition.OnGrab;
            resource.SupportsOnGrab = definition.SupportsOnGrab;
            resource.OnDownload = definition.OnDownload;
            resource.SupportsOnDownload = definition.SupportsOnDownload;
            resource.OnLiveStreamStart = definition.OnLiveStreamStart;
            resource.SupportsOnLiveStreamStart = definition.SupportsOnLiveStreamStart;
            resource.OnLiveStreamEnd = definition.OnLiveStreamEnd;
            resource.SupportsOnLiveStreamEnd = definition.SupportsOnLiveStreamEnd;
            resource.OnChannelAdded = definition.OnChannelAdded;
            resource.SupportsOnChannelAdded = definition.SupportsOnChannelAdded;
            return resource;
        }

        public override NotificationDefinition ToModel(NotificationResource resource, NotificationDefinition? existingDefinition)
        {
            var definition = base.ToModel(resource, existingDefinition);
            definition.Enable = resource.Enable;
            definition.OnGrab = resource.OnGrab;
            definition.OnDownload = resource.OnDownload;
            definition.OnLiveStreamStart = resource.OnLiveStreamStart;
            definition.OnLiveStreamEnd = resource.OnLiveStreamEnd;
            definition.OnChannelAdded = resource.OnChannelAdded;
            return definition;
        }
    }
}
