using System.Collections.Generic;
using Streamarr.Core.MediaFiles;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Organizer
{
    public class SampleResult
    {
        public string FileName { get; set; }
        public Series Series { get; set; }
        public List<Episode> Episodes { get; set; }
        public EpisodeFile EpisodeFile { get; set; }
    }
}
