using Streamarr.Core.Extras.Metadata;
using Streamarr.Api.V5.Provider;

namespace Streamarr.Api.V5.Metadata;

public class MetadataBulkResource : ProviderBulkResource<MetadataBulkResource>
{
}

public class MetadataBulkResourceMapper : ProviderBulkResourceMapper<MetadataBulkResource, MetadataDefinition>
{
}
