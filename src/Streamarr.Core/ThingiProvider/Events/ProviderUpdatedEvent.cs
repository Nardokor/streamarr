using Streamarr.Common.Messaging;

namespace Streamarr.Core.ThingiProvider.Events
{
    public class ProviderUpdatedEvent<TProvider> : IEvent
    {
        public ProviderDefinition Definition { get; private set; }

        public ProviderUpdatedEvent(ProviderDefinition definition)
        {
            Definition = definition;
        }
    }
}
