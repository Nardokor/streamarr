using Streamarr.Core.History;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.History;

public class HistoryResource : RestResource
{
    public int ContentId { get; set; }
    public int ChannelId { get; set; }
    public int CreatorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public static class HistoryResourceMapper
{
    public static HistoryResource ToResource(this DownloadHistory model)
    {
        return new HistoryResource
        {
            Id = model.Id,
            ContentId = model.ContentId,
            ChannelId = model.ChannelId,
            CreatorId = model.CreatorId,
            Title = model.Title,
            EventType = model.EventType.ToString(),
            Data = model.Data,
            Date = model.Date,
        };
    }
}
