using Streamarr.Api.V1.Provider;
using Streamarr.Core.Channels;
using Streamarr.Core.MetadataSource;

namespace Streamarr.Api.V1.MetadataSources
{
    public class MetadataSourceResource : ProviderResource<MetadataSourceResource>
    {
        public PlatformType Platform { get; set; }
        public bool Enable { get; set; }
    }

    public class MetadataSourceBulkResource : ProviderBulkResource<MetadataSourceBulkResource>
    {
    }

    public class MetadataSourceResourceMapper : ProviderResourceMapper<MetadataSourceResource, MetadataSourceDefinition>
    {
        public override MetadataSourceResource ToResource(MetadataSourceDefinition definition)
        {
            var resource = base.ToResource(definition);
            resource.Platform = definition.Platform;
            resource.Enable = definition.Enable;
            return resource;
        }

        public override MetadataSourceDefinition ToModel(MetadataSourceResource resource, MetadataSourceDefinition? existingDefinition)
        {
            var definition = base.ToModel(resource, existingDefinition);
            definition.Enable = resource.Enable;
            return definition;
        }
    }

    public class MetadataSourceBulkResourceMapper : ProviderBulkResourceMapper<MetadataSourceBulkResource, MetadataSourceDefinition>
    {
    }
}
