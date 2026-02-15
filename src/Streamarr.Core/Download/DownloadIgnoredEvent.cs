using System.Collections.Generic;
using Streamarr.Common.Messaging;
using Streamarr.Core.Download.TrackedDownloads;
using Streamarr.Core.Languages;
using Streamarr.Core.Qualities;

namespace Streamarr.Core.Download
{
    public class DownloadIgnoredEvent : IEvent
    {
        public int SeriesId { get; set; }
        public List<int> EpisodeIds { get; set; }
        public List<Language> Languages { get; set; }
        public QualityModel Quality { get; set; }
        public string SourceTitle { get; set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; set; }
        public TrackedDownload TrackedDownload { get; set; }
        public string Message { get; set; }
    }
}
