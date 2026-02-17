using Streamarr.Core.Profiles.Qualities;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Profiles.Quality;

public class QualityProfileResource : RestResource
{
    public string? Name { get; set; }
    public bool UpgradeAllowed { get; set; }
    public int Cutoff { get; set; }
    public List<QualityProfileQualityItemResource> Items { get; set; } = [];
}

public class QualityProfileQualityItemResource : RestResource
{
    public string? Name { get; set; }
    public Streamarr.Core.Qualities.Quality? Quality { get; set; }
    public List<QualityProfileQualityItemResource> Items { get; set; } = [];
    public bool Allowed { get; set; }
    public double? MinSize { get; set; }
    public double? MaxSize { get; set; }
    public double? PreferredSize { get; set; }
}

public static class ProfileResourceMapper
{
    public static QualityProfileResource ToResource(this QualityProfile model)
    {
        return new QualityProfileResource
        {
            Id = model.Id,
            Name = model.Name,
            UpgradeAllowed = model.UpgradeAllowed,
            Cutoff = model.Cutoff,
            Items = model.Items.ConvertAll(ToResource)
        };
    }

    public static QualityProfileQualityItemResource ToResource(this QualityProfileQualityItem model)
    {
        return new QualityProfileQualityItemResource
        {
            Id = model.Id,
            Name = model.Name,
            Quality = model.Quality,
            Items = model.Items.ConvertAll(ToResource),
            Allowed = model.Allowed,
            MinSize = model.MinSize,
            MaxSize = model.MaxSize,
            PreferredSize = model.PreferredSize
        };
    }

    public static QualityProfile ToModel(this QualityProfileResource resource)
    {
        return new QualityProfile
        {
            Id = resource.Id,
            Name = resource.Name,
            UpgradeAllowed = resource.UpgradeAllowed,
            Cutoff = resource.Cutoff,
            Items = resource.Items.ConvertAll(ToModel)
        };
    }

    public static QualityProfileQualityItem ToModel(this QualityProfileQualityItemResource resource)
    {
        return new QualityProfileQualityItem
        {
            Id = resource.Id,
            Name = resource.Name,
            Quality = resource.Quality != null ? (Streamarr.Core.Qualities.Quality)resource.Quality.Id : null,
            Items = resource.Items.ConvertAll(ToModel),
            Allowed = resource.Allowed,
            MinSize = resource.MinSize,
            MaxSize = resource.MaxSize,
            PreferredSize = resource.PreferredSize
        };
    }

    public static List<QualityProfileResource> ToResource(this IEnumerable<QualityProfile> models)
    {
        return models.Select(ToResource).ToList();
    }
}
