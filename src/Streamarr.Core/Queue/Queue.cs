using System;
using System.Collections.Generic;
using Streamarr.Core.Datastore;
using Streamarr.Core.Download.TrackedDownloads;
using Streamarr.Core.Indexers;
using Streamarr.Core.Languages;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Qualities;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Queue
{
    public class Queue : ModelBase
    {
        public Series Series { get; set; }

        public int? SeasonNumber { get; set; }

        [Obsolete]
        public Episode Episode { get; set; }

        public List<Episode> Episodes { get; set; }
        public List<Language> Languages { get; set; }
        public QualityModel Quality { get; set; }
        public decimal Size { get; set; }
        public string Title { get; set; }
        public decimal SizeLeft { get; set; }
        public TimeSpan? TimeLeft { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public DateTime? Added { get; set; }
        public QueueStatus Status { get; set; }
        public TrackedDownloadStatus? TrackedDownloadStatus { get; set; }
        public TrackedDownloadState? TrackedDownloadState { get; set; }
        public List<TrackedDownloadStatusMessage> StatusMessages { get; set; }
        public string DownloadId { get; set; }
        public RemoteEpisode RemoteEpisode { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public string DownloadClient { get; set; }
        public bool DownloadClientHasPostImportCategory { get; set; }
        public string Indexer { get; set; }
        public string OutputPath { get; set; }
        public string ErrorMessage { get; set; }
    }
}
