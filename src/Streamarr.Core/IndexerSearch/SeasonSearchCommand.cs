using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.IndexerSearch
{
    public class SeasonSearchCommand : Command
    {
        public int SeriesId { get; set; }
        public int SeasonNumber { get; set; }

        public override bool SendUpdatesToClient => true;
    }
}
