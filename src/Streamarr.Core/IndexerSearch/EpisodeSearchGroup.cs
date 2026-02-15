using System.Collections.Generic;
using Streamarr.Core.Tv;

namespace Streamarr.Core.IndexerSearch
{
    public class EpisodeSearchGroup
    {
        public int SeriesId { get; set; }
        public int SeasonNumber { get; set; }
        public List<Episode> Episodes { get; set; }
    }
}
