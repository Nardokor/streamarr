using Streamarr.Common.Messaging;

namespace Streamarr.Core.Notifications
{
    public class ContentDownloadedEvent : IEvent
    {
        public ContentDownloadedMessage Message { get; set; }
    }
}
