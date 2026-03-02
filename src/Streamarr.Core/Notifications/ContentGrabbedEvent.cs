using Streamarr.Common.Messaging;

namespace Streamarr.Core.Notifications
{
    public class ContentGrabbedEvent : IEvent
    {
        public ContentGrabbedMessage Message { get; set; }
    }
}
