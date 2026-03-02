using System;
using System.Collections.Generic;
using FluentValidation.Results;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.Notifications
{
    public abstract class NotificationBase<TSettings> : INotification
        where TSettings : NotificationSettingsBase<TSettings>
    {
        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public virtual IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }

        protected TSettings Settings => (TSettings)Definition.Settings;

        public virtual void OnGrab(ContentGrabbedMessage message)
        {
        }

        public virtual bool SupportsOnGrab => true;

        public virtual void OnDownload(ContentDownloadedMessage message)
        {
        }

        public virtual bool SupportsOnDownload => true;

        public virtual void OnLiveStreamStart(LiveStreamStartedMessage message)
        {
        }

        public virtual bool SupportsOnLiveStreamStart => true;

        public virtual void OnLiveStreamEnd(LiveStreamEndedMessage message)
        {
        }

        public virtual bool SupportsOnLiveStreamEnd => true;

        public virtual void OnChannelAdded(ChannelAddedMessage message)
        {
        }

        public virtual bool SupportsOnChannelAdded => true;

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
