using System;
using System.Collections.Generic;
using FluentValidation.Results;
using Streamarr.Core.Channels;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.MetadataSource
{
    public abstract class MetadataSourceBase<TSettings> : IMetadataSource
        where TSettings : MetadataSourceSettingsBase
    {
        public abstract string Name { get; }
        public abstract PlatformType Platform { get; }

        public Type ConfigContract => typeof(TSettings);
        public virtual ProviderMessage Message => null;
        public virtual IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();
        public ProviderDefinition Definition { get; set; }

        protected TSettings Settings => (TSettings)Definition.Settings;

        public abstract CreatorMetadataResult SearchCreator(string query);
        public abstract ChannelMetadataResult GetChannelMetadata(string platformUrl);
        public abstract IEnumerable<ContentMetadataResult> GetNewContent(string platformUrl, string platformId, DateTime? since);
        public abstract ContentMetadataResult GetContentMetadata(string platformContentId);
        public abstract IEnumerable<ContentMetadataResult> GetContentMetadataBatch(IEnumerable<string> platformContentIds);
        public abstract IEnumerable<ContentStatusUpdate> GetLivestreamStatusUpdates(IEnumerable<string> platformContentIds);

        public virtual ValidationResult Test()
        {
            return new ValidationResult();
        }

        public virtual object RequestAction(string stage, IDictionary<string, string> query)
        {
            return null;
        }
    }
}
