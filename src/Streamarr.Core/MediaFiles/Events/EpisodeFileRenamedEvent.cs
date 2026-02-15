using Streamarr.Common.Messaging;
using Streamarr.Core.Tv;

namespace Streamarr.Core.MediaFiles.Events
{
    public class EpisodeFileRenamedEvent : IEvent
    {
        public Series Series { get; private set; }
        public EpisodeFile EpisodeFile { get; private set; }
        public string OriginalPath { get; private set; }

        public EpisodeFileRenamedEvent(Series series, EpisodeFile episodeFile, string originalPath)
        {
            Series = series;
            EpisodeFile = episodeFile;
            OriginalPath = originalPath;
        }
    }
}
