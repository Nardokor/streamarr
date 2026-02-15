using System.Collections.Generic;
using Streamarr.Core.Languages;
using Streamarr.Core.Qualities;

namespace Streamarr.Api.V3.EpisodeFiles
{
    public class EpisodeFileListResource
    {
        public List<int> EpisodeFileIds { get; set; }
        public List<Language> Languages { get; set; }
        public QualityModel Quality { get; set; }
        public string SceneName { get; set; }
        public string ReleaseGroup { get; set; }
    }
}
