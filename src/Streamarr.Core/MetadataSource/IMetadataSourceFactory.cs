#nullable enable
using Streamarr.Core.Channels;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.MetadataSource
{
    public interface IMetadataSourceFactory : IProviderFactory<IMetadataSource, MetadataSourceDefinition>
    {
        IMetadataSource? GetByPlatform(PlatformType platform);
    }
}
