using Streamarr.Api.V5.Provider;
using Streamarr.Core.Extras.Metadata;

namespace Streamarr.Api.V5.Metadata;

public class MetadataBulkResource : ProviderBulkResource<MetadataBulkResource>
{
}

public class MetadataBulkResourceMapper : ProviderBulkResourceMapper<MetadataBulkResource, MetadataDefinition>
{
}
