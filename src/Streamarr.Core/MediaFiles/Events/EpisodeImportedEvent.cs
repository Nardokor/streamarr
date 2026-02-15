using System.Collections.Generic;
using Streamarr.Common.Messaging;
using Streamarr.Core.Download;
using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.MediaFiles.Events
{
    public class EpisodeImportedEvent : IEvent
    {
        public LocalEpisode EpisodeInfo { get; private set; }
        public EpisodeFile ImportedEpisode { get; private set; }
        public List<DeletedEpisodeFile> OldFiles { get; private set; }
        public bool NewDownload { get; private set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; private set; }

        public EpisodeImportedEvent(LocalEpisode episodeInfo, EpisodeFile importedEpisode, List<DeletedEpisodeFile> oldFiles, bool newDownload, DownloadClientItem downloadClientItem)
        {
            EpisodeInfo = episodeInfo;
            ImportedEpisode = importedEpisode;
            OldFiles = oldFiles;
            NewDownload = newDownload;

            if (downloadClientItem != null)
            {
                DownloadClientInfo = downloadClientItem.DownloadClientInfo;
                DownloadId = downloadClientItem.DownloadId;
            }
        }
    }
}
