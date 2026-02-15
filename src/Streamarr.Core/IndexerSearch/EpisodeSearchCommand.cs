using System.Collections.Generic;
using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.IndexerSearch
{
    public class EpisodeSearchCommand : Command
    {
        public List<int> EpisodeIds { get; set; }

        public override bool SendUpdatesToClient => true;

        public EpisodeSearchCommand()
        {
        }

        public EpisodeSearchCommand(List<int> episodeIds)
        {
            EpisodeIds = episodeIds;
        }
    }
}
