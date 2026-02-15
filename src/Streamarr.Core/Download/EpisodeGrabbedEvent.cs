using Streamarr.Common.Messaging;
using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.Download
{
    public class EpisodeGrabbedEvent : IEvent
    {
        public RemoteEpisode Episode { get; private set; }
        public int DownloadClientId { get; set; }
        public string DownloadClient { get; set; }
        public string DownloadClientName { get; set; }
        public string DownloadId { get; set; }

        public EpisodeGrabbedEvent(RemoteEpisode episode)
        {
            Episode = episode;
        }
    }
}
