using System.Collections.Generic;
using Streamarr.Common.Messaging;
using Streamarr.Core.Download.TrackedDownloads;
using Streamarr.Core.MediaFiles;
using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.Download
{
    public class DownloadCompletedEvent : IEvent
    {
        public TrackedDownload TrackedDownload { get; private set; }
        public int SeriesId { get; private set; }
        public List<EpisodeFile> EpisodeFiles { get; private set; }
        public GrabbedReleaseInfo Release { get; private set; }

        public DownloadCompletedEvent(TrackedDownload trackedDownload, int seriesId, List<EpisodeFile> episodeFiles, GrabbedReleaseInfo release)
        {
            TrackedDownload = trackedDownload;
            SeriesId = seriesId;
            EpisodeFiles = episodeFiles;
            Release = release;
        }
    }
}
