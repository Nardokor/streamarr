using Streamarr.Core.Content;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Contents;

public class ContentResource : RestResource
{
    public int ChannelId { get; set; }
    public int ContentFileId { get; set; }
    public string PlatformContentId { get; set; } = string.Empty;
    public ContentType ContentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public TimeSpan? Duration { get; set; }
    public DateTime? AirDateUtc { get; set; }
    public DateTime DateAdded { get; set; }
    public bool Monitored { get; set; }
    public bool IsMembers { get; set; }
    public bool IsAccessible { get; set; }
    public ContentStatus Status { get; set; }

    // Populated only on single-item requests (no N+1 for lists)
    public string? FileRelativePath { get; set; }
    public long? FileSize { get; set; }
}

public static class ContentResourceMapper
{
    public static ContentResource ToResource(this Content model)
    {
        return new ContentResource
        {
            Id = model.Id,
            ChannelId = model.ChannelId,
            ContentFileId = model.ContentFileId,
            PlatformContentId = model.PlatformContentId,
            ContentType = model.ContentType,
            Title = model.Title,
            Description = model.Description,
            ThumbnailUrl = model.ThumbnailUrl,
            Duration = model.Duration,
            AirDateUtc = model.AirDateUtc,
            DateAdded = model.DateAdded,
            Monitored = model.Monitored,
            IsMembers = model.IsMembers,
            IsAccessible = model.IsAccessible,
            Status = model.Status
        };
    }

    public static Content ToModel(this ContentResource resource)
    {
        return new Content
        {
            Id = resource.Id,
            ChannelId = resource.ChannelId,
            ContentFileId = resource.ContentFileId,
            PlatformContentId = resource.PlatformContentId,
            ContentType = resource.ContentType,
            Title = resource.Title,
            Description = resource.Description,
            ThumbnailUrl = resource.ThumbnailUrl,
            Duration = resource.Duration,
            AirDateUtc = resource.AirDateUtc,
            Monitored = resource.Monitored,
            Status = resource.Status
        };
    }

    public static List<ContentResource> ToResource(this IEnumerable<Content> models)
    {
        return models.Select(ToResource).ToList();
    }
}
