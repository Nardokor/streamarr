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
}
