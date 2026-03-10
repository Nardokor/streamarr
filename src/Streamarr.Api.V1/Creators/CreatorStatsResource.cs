namespace Streamarr.Api.V1.Creators;

public class CreatorStatsResource
{
    public int CreatorId { get; set; }
    public int DownloadedCount { get; set; }
    public int WantedCount { get; set; }
    public bool IsLiveNow { get; set; }
    public bool HasMissing { get; set; }
    public bool HasActiveMembership { get; set; }
}
