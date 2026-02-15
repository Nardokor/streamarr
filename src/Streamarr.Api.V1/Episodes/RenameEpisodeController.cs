using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.MediaFiles;
using Streamarr.Http;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Episodes;

[V1ApiController("rename")]
public class RenameEpisodeController : Controller
{
    private readonly IRenameEpisodeFileService _renameEpisodeFileService;

    public RenameEpisodeController(IRenameEpisodeFileService renameEpisodeFileService)
    {
        _renameEpisodeFileService = renameEpisodeFileService;
    }

    [HttpGet]
    [Produces("application/json")]
    public List<RenameEpisodeResource> GetEpisodes(int seriesId, int? seasonNumber)
    {
        if (seasonNumber.HasValue)
        {
            return _renameEpisodeFileService.GetRenamePreviews(seriesId, seasonNumber.Value).ToResource();
        }

        return _renameEpisodeFileService.GetRenamePreviews(seriesId).ToResource();
    }

    [HttpGet("bulk")]
    [Produces("application/json")]
    public List<RenameEpisodeResource> GetEpisodes([FromQuery] List<int> seriesIds)
    {
        if (seriesIds is { Count: 0 })
        {
            throw new BadRequestException("seriesIds must be provided");
        }

        if (seriesIds.Any(seriesId => seriesId <= 0))
        {
            throw new BadRequestException("seriesIds must be positive integers");
        }

        return _renameEpisodeFileService.GetRenamePreviews(seriesIds).ToResource();
    }
}
