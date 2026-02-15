namespace Streamarr.Api.V1.Episodes;

public class EpisodesMonitoredResource
{
    public required List<int> EpisodeIds { get; set; }
    public bool Monitored { get; set; }
}
