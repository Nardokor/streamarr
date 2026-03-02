using Streamarr.Common.Messaging;

namespace Streamarr.Core.Notifications
{
    public class LiveStreamEndedEvent : IEvent
    {
        public LiveStreamEndedMessage Message { get; set; }
    }
}
