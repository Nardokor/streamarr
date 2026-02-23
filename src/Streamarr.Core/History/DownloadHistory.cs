using System;
using Streamarr.Core.Datastore;
using Streamarr.Core.Qualities;

namespace Streamarr.Core.History
{
    public enum DownloadHistoryEventType
    {
        Downloaded = 1,
        DownloadFailed = 2,
        Deleted = 3,
        Ignored = 4,
    }

    public class DownloadHistory : ModelBase
    {
        public int ContentId { get; set; }
        public int ChannelId { get; set; }
        public int CreatorId { get; set; }
        public string Title { get; set; } = string.Empty;
        public QualityModel Quality { get; set; } = new QualityModel();
        public DownloadHistoryEventType EventType { get; set; }
        public string Data { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
