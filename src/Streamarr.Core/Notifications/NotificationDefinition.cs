using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.Notifications
{
    public class NotificationDefinition : ProviderDefinition
    {
        public bool OnGrab { get; set; }
        public bool SupportsOnGrab { get; set; }

        public bool OnDownload { get; set; }
        public bool SupportsOnDownload { get; set; }

        public bool OnLiveStreamStart { get; set; }
        public bool SupportsOnLiveStreamStart { get; set; }

        public bool OnLiveStreamEnd { get; set; }
        public bool SupportsOnLiveStreamEnd { get; set; }

        public bool OnChannelAdded { get; set; }
        public bool SupportsOnChannelAdded { get; set; }
    }
}
