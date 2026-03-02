using Streamarr.Common.Messaging;

namespace Streamarr.Core.Notifications
{
    public class LiveStreamStartedEvent : IEvent
    {
        public LiveStreamStartedMessage Message { get; set; }
    }
}
