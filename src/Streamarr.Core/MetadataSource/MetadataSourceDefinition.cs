using Streamarr.Core.Channels;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.MetadataSource
{
    public class MetadataSourceDefinition : ProviderDefinition
    {
        public PlatformType Platform { get; set; }
    }
}
