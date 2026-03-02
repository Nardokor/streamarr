using Streamarr.Core.Channels;

namespace Streamarr.Core.Notifications
{
    public class ChannelAddedMessage
    {
        public string ChannelTitle { get; set; }
        public string CreatorName { get; set; }
        public PlatformType Platform { get; set; }
    }
}
