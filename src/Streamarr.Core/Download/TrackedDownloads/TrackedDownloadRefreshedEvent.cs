using System.Collections.Generic;
using Streamarr.Common.Messaging;

namespace Streamarr.Core.Download.TrackedDownloads
{
    public class TrackedDownloadRefreshedEvent : IEvent
    {
        public List<TrackedDownload> TrackedDownloads { get; private set; }

        public TrackedDownloadRefreshedEvent(List<TrackedDownload> trackedDownloads)
        {
            TrackedDownloads = trackedDownloads;
        }
    }
}
