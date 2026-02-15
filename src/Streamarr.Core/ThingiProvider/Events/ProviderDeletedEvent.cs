using Streamarr.Common.Messaging;

namespace Streamarr.Core.ThingiProvider.Events
{
    public class ProviderDeletedEvent<TProvider> : IEvent
    {
        public int ProviderId { get; private set; }

        public ProviderDeletedEvent(int id)
        {
            ProviderId = id;
        }
    }
}
