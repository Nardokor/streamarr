using Streamarr.Common.Messaging;

namespace Streamarr.Core.MediaFiles.Events
{
    public class EpisodeFileAddedEvent : IEvent
    {
        public EpisodeFile EpisodeFile { get; private set; }

        public EpisodeFileAddedEvent(EpisodeFile episodeFile)
        {
            EpisodeFile = episodeFile;
        }
    }
}
