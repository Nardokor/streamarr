using System.Collections.Generic;
using Streamarr.Core.Download;
using Streamarr.Core.MediaFiles;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Notifications
{
    public class DownloadMessage
    {
        public string Message { get; set; }
        public Series Series { get; set; }
        public LocalEpisode EpisodeInfo { get; set; }
        public EpisodeFile EpisodeFile { get; set; }
        public List<DeletedEpisodeFile> OldFiles { get; set; }
        public string SourcePath { get; set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; set; }
        public GrabbedReleaseInfo Release { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
