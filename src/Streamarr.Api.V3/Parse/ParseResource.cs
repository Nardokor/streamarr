using System.Collections.Generic;
using Streamarr.Core.Languages;
using Streamarr.Core.Parser.Model;
using Streamarr.Api.V3.CustomFormats;
using Streamarr.Api.V3.Episodes;
using Streamarr.Api.V3.Series;
using Streamarr.Http.REST;

namespace Streamarr.Api.V3.Parse
{
    public class ParseResource : RestResource
    {
        public string Title { get; set; }
        public ParsedEpisodeInfo ParsedEpisodeInfo { get; set; }
        public SeriesResource Series { get; set; }
        public List<EpisodeResource> Episodes { get; set; }
        public List<Language> Languages { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
    }
}
