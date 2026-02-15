using Streamarr.Common.Messaging;

namespace Streamarr.Core.MediaFiles.Events
{
    public class EpisodeFileDeletedEvent : IEvent
    {
        public EpisodeFile EpisodeFile { get; private set; }
        public DeleteMediaFileReason Reason { get; private set; }

        public EpisodeFileDeletedEvent(EpisodeFile episodeFile, DeleteMediaFileReason reason)
        {
            EpisodeFile = episodeFile;
            Reason = reason;
        }
    }
}
