using Streamarr.Api.V1.Provider;
using Streamarr.Core.Extras.Metadata;

namespace Streamarr.Api.V1.Metadata;

public class MetadataBulkResource : ProviderBulkResource<MetadataBulkResource>
{
}

public class MetadataBulkResourceMapper : ProviderBulkResourceMapper<MetadataBulkResource, MetadataDefinition>
{
}
