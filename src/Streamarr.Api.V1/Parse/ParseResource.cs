using Streamarr.Api.V1.CustomFormats;
using Streamarr.Api.V1.Episodes;
using Streamarr.Api.V1.Series;
using Streamarr.Core.Languages;
using Streamarr.Core.Parser.Model;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Parse;

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
