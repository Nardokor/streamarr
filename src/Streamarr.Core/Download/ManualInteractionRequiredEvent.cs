using Streamarr.Common.Messaging;
using Streamarr.Core.Download.TrackedDownloads;
using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.Download
{
    public class ManualInteractionRequiredEvent : IEvent
    {
        public RemoteEpisode Episode { get; private set; }
        public TrackedDownload TrackedDownload { get; private set; }
        public GrabbedReleaseInfo Release { get; private set; }

        public ManualInteractionRequiredEvent(TrackedDownload trackedDownload, GrabbedReleaseInfo release)
        {
            TrackedDownload = trackedDownload;
            Episode = trackedDownload.RemoteEpisode;
            Release = release;
        }
    }
}
