namespace Streamarr.Core.Notifications
{
    public class LiveStreamEndedMessage
    {
        public string ContentTitle { get; set; }
        public string CreatorName { get; set; }
        public string ChannelName { get; set; }
        public long FileSize { get; set; }
    }
}
