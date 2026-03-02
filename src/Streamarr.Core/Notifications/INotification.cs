using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.Notifications
{
    public interface INotification : IProvider
    {
        void OnGrab(ContentGrabbedMessage message);
        bool SupportsOnGrab { get; }

        void OnDownload(ContentDownloadedMessage message);
        bool SupportsOnDownload { get; }

        void OnLiveStreamStart(LiveStreamStartedMessage message);
        bool SupportsOnLiveStreamStart { get; }

        void OnLiveStreamEnd(LiveStreamEndedMessage message);
        bool SupportsOnLiveStreamEnd { get; }

        void OnChannelAdded(ChannelAddedMessage message);
        bool SupportsOnChannelAdded { get; }
    }
}
