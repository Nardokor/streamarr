using Streamarr.Core.Content;

namespace Streamarr.Core.Notifications
{
    public class ContentGrabbedMessage
    {
        public string ContentTitle { get; set; }
        public string CreatorName { get; set; }
        public string ChannelName { get; set; }
        public ContentType ContentType { get; set; }
    }
}
