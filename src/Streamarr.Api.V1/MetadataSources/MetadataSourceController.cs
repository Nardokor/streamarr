using Streamarr.Api.V1.Provider;
using Streamarr.Core.MetadataSource;
using Streamarr.Http;
using Streamarr.SignalR;

namespace Streamarr.Api.V1.MetadataSources
{
    [V1ApiController]
    public class MetadataSourceController : ProviderControllerBase<MetadataSourceResource, MetadataSourceBulkResource, IMetadataSource, MetadataSourceDefinition>
    {
        public MetadataSourceController(MetadataSourceFactory metadataSourceFactory,
                                        IBroadcastSignalRMessage signalRBroadcaster)
            : base(signalRBroadcaster,
                   metadataSourceFactory,
                   "metadatasource",
                   new MetadataSourceResourceMapper(),
                   new MetadataSourceBulkResourceMapper())
        {
        }
    }
}
