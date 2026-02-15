using Streamarr.Api.V1.Series;

namespace Streamarr.Api.V1.SeasonPass;

public class SeasonPassSeriesResource
{
    public int Id { get; set; }
    public bool? Monitored { get; set; }
    public List<SeasonResource> Seasons { get; set; } = [];
}
