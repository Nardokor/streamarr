using System.Collections.Generic;
using Streamarr.Core.Download;
using Streamarr.Core.MediaFiles;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Qualities;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Notifications
{
    public class ImportCompleteMessage
    {
        public string Message { get; set; }
        public Series Series { get; set; }
        public List<Episode> Episodes { get; set; }
        public List<EpisodeFile> EpisodeFiles { get; set; }
        public string SourcePath { get; set; }
        public string SourceTitle { get; set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; set; }
        public GrabbedReleaseInfo Release { get; set; }
        public string DestinationPath { get; set; }
        public string ReleaseGroup { get; set; }
        public QualityModel ReleaseQuality { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
