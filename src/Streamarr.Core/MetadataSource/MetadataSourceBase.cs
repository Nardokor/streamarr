#nullable enable
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

        // By default, livestream status checks use this source's own platform.
        // Override to delegate to another platform (e.g. Fourthwall → YouTube).
        public virtual PlatformType LivestreamDelegatePlatform => Platform;

        public Type ConfigContract => typeof(TSettings);
        public virtual ProviderMessage Message => null!;
        public virtual IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();
        public ProviderDefinition Definition { get; set; } = null!;

        protected TSettings Settings => (TSettings)Definition.Settings;

        public abstract CreatorMetadataResult SearchCreator(string query);
        public abstract ChannelMetadataResult GetChannelMetadata(string platformUrl);
        public abstract IEnumerable<ContentMetadataResult> GetNewContent(string platformUrl, string platformId, DateTime? since, bool checkMembership = false);
        public abstract ContentMetadataResult? GetContentMetadata(string platformContentId);
        public abstract IEnumerable<ContentMetadataResult> GetContentMetadataBatch(IEnumerable<string> platformContentIds);
        public abstract IEnumerable<ContentStatusUpdate> GetLivestreamStatusUpdates(IEnumerable<string> platformContentIds);

        public virtual ContentAccessibilityResult ProbeContentAccessibility(string platformContentId, bool withCookies = true)
        {
            return ContentAccessibilityResult.Accessible();
        }

        public virtual ContentMetadataResult? GetActiveLivestream(string platformUrl, string platformId)
        {
            return null;
        }

        public abstract string GetDownloadUrl(string platformContentId);

        public virtual ValidationResult Test()
        {
            return new ValidationResult();
        }

        public virtual object RequestAction(string stage, IDictionary<string, string> query)
        {
            return null!;
        }
    }
}
