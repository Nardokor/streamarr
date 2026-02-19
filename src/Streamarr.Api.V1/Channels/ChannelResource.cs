using Streamarr.Core.Channels;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Channels;

public class ChannelResource : RestResource
{
    public int CreatorId { get; set; }
    public PlatformType Platform { get; set; }
    public string PlatformId { get; set; } = string.Empty;
    public string PlatformUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public bool Monitored { get; set; }
    public ChannelStatusType Status { get; set; }
    public DateTime? LastInfoSync { get; set; }
}

public static class ChannelResourceMapper
{
    public static ChannelResource ToResource(this Channel model)
    {
        return new ChannelResource
        {
            Id = model.Id,
            CreatorId = model.CreatorId,
            Platform = model.Platform,
            PlatformId = model.PlatformId,
            PlatformUrl = model.PlatformUrl,
            Title = model.Title,
            Description = model.Description,
            ThumbnailUrl = model.ThumbnailUrl,
            Monitored = model.Monitored,
            Status = model.Status,
            LastInfoSync = model.LastInfoSync
        };
    }

    public static Channel ToModel(this ChannelResource resource)
    {
        return new Channel
        {
            Id = resource.Id,
            CreatorId = resource.CreatorId,
            Platform = resource.Platform,
            PlatformId = resource.PlatformId,
            PlatformUrl = resource.PlatformUrl,
            Title = resource.Title,
            Description = resource.Description,
            ThumbnailUrl = resource.ThumbnailUrl,
            Monitored = resource.Monitored,
            Status = resource.Status
        };
    }

    public static List<ChannelResource> ToResource(this IEnumerable<Channel> models)
    {
        return models.Select(ToResource).ToList();
    }
}
