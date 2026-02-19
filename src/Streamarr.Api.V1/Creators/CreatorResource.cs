using Streamarr.Core.Creators;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Creators;

public class CreatorResource : RestResource
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string RootFolderPath { get; set; } = string.Empty;
    public int QualityProfileId { get; set; }
    public HashSet<int> Tags { get; set; } = new HashSet<int>();
    public bool Monitored { get; set; }
    public CreatorStatusType Status { get; set; }
    public DateTime Added { get; set; }
    public DateTime? LastInfoSync { get; set; }
}

public static class CreatorResourceMapper
{
    public static CreatorResource ToResource(this Creator model)
    {
        return new CreatorResource
        {
            Id = model.Id,
            Title = model.Title,
            Description = model.Description,
            ThumbnailUrl = model.ThumbnailUrl,
            Path = model.Path,
            RootFolderPath = model.RootFolderPath,
            QualityProfileId = model.QualityProfileId,
            Tags = model.Tags,
            Monitored = model.Monitored,
            Status = model.Status,
            Added = model.Added,
            LastInfoSync = model.LastInfoSync
        };
    }

    public static Creator ToModel(this CreatorResource resource)
    {
        return new Creator
        {
            Id = resource.Id,
            Title = resource.Title,
            Description = resource.Description,
            ThumbnailUrl = resource.ThumbnailUrl,
            Path = resource.Path,
            QualityProfileId = resource.QualityProfileId,
            Tags = resource.Tags ?? new HashSet<int>(),
            Monitored = resource.Monitored,
            Status = resource.Status
        };
    }

    public static List<CreatorResource> ToResource(this IEnumerable<Creator> models)
    {
        return models.Select(ToResource).ToList();
    }
}
