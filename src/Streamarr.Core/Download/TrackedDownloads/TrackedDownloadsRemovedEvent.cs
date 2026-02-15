using System.Collections.Generic;
using Streamarr.Common.Messaging;

namespace Streamarr.Core.Download.TrackedDownloads
{
    public class TrackedDownloadsRemovedEvent : IEvent
    {
        public List<TrackedDownload> TrackedDownloads { get; private set; }

        public TrackedDownloadsRemovedEvent(List<TrackedDownload> trackedDownloads)
        {
            TrackedDownloads = trackedDownloads;
        }
    }
}
