using Streamarr.Common.Messaging;

namespace Streamarr.Core.Notifications
{
    public class ChannelAddedEvent : IEvent
    {
        public ChannelAddedMessage Message { get; set; }
    }
}
