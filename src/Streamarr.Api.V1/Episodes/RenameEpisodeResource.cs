using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Episodes;

public class RenameEpisodeResource : RestResource
{
    public int SeriesId { get; set; }
    public int SeasonNumber { get; set; }
    public List<int> EpisodeNumbers { get; set; } = [];
    public int EpisodeFileId { get; set; }
    public string? ExistingPath { get; set; }
    public string? NewPath { get; set; }
}

public static class RenameEpisodeResourceMapper
{
    public static RenameEpisodeResource ToResource(this Streamarr.Core.MediaFiles.RenameEpisodeFilePreview model)
    {
        return new RenameEpisodeResource
        {
            Id = model.EpisodeFileId,
            SeriesId = model.SeriesId,
            SeasonNumber = model.SeasonNumber,
            EpisodeNumbers = model.EpisodeNumbers.ToList(),
            EpisodeFileId = model.EpisodeFileId,
            ExistingPath = model.ExistingPath,
            NewPath = model.NewPath
        };
    }

    public static List<RenameEpisodeResource> ToResource(this IEnumerable<Streamarr.Core.MediaFiles.RenameEpisodeFilePreview> models)
    {
        return models.Select(ToResource).ToList();
    }
}
