namespace Streamarr.Api.V1.Queue;

public class QueueResource
{
    public int CommandId { get; set; }
    public int ContentId { get; set; }
    public string ContentTitle { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }

    // One of: queued, waitingForSlot, downloading, liveWaiting — see QueueController.ResolveState.
    public string State { get; set; } = string.Empty;
}

public class QueueSlotsResource
{
    public int ConfiguredMax { get; set; }
    public int EffectiveMax { get; set; }
    public int AvailableSlots { get; set; }
    public List<int> ActiveDownloadContentIds { get; set; } = new();
    public List<int> LiveWaitingContentIds { get; set; } = new();
}
