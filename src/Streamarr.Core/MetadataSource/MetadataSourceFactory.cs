#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.MetadataSource
{
    public class MetadataSourceFactory : ProviderFactory<IMetadataSource, MetadataSourceDefinition>, IMetadataSourceFactory
    {
        public MetadataSourceFactory(MetadataSourceRepository repository,
                                     IEnumerable<IMetadataSource> providers,
                                     IServiceProvider container,
                                     IEventAggregator eventAggregator,
                                     Logger logger)
            : base(repository, providers, container, eventAggregator, logger)
        {
        }

        public IMetadataSource? GetByPlatform(PlatformType platform)
        {
            var def = All().FirstOrDefault(d => d.Platform == platform && d.Enable);
            return def != null ? GetInstance(def) : null;
        }

        public override void SetProviderCharacteristics(IMetadataSource provider, MetadataSourceDefinition definition)
        {
            base.SetProviderCharacteristics(provider, definition);
            definition.Platform = provider.Platform;
        }
    }
}
