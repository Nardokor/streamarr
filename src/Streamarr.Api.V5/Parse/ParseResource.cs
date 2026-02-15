using Streamarr.Api.V5.CustomFormats;
using Streamarr.Api.V5.Episodes;
using Streamarr.Api.V5.Series;
using Streamarr.Core.Languages;
using Streamarr.Core.Parser.Model;
using Streamarr.Http.REST;

namespace Streamarr.Api.V5.Parse;

public class ParseResource : RestResource
{
    public string? Title { get; set; }
    public ParsedEpisodeInfo? ParsedEpisodeInfo { get; set; }
    public SeriesResource? Series { get; set; }
    public List<EpisodeResource>? Episodes { get; set; }
    public List<Language>? Languages { get; set; }
    public List<CustomFormatResource>? CustomFormats { get; set; }
    public int CustomFormatScore { get; set; }
}
