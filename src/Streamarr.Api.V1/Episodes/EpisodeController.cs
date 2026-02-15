using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.CustomFormats;
using Streamarr.Core.DecisionEngine.Specifications;
using Streamarr.Core.Tv;
using Streamarr.Http;
using Streamarr.Http.REST;
using Streamarr.Http.REST.Attributes;
using Streamarr.SignalR;

namespace Streamarr.Api.V1.Episodes;

[V1ApiController]
public class EpisodeController : EpisodeControllerWithSignalR
{
    public EpisodeController(ISeriesService seriesService,
                         IEpisodeService episodeService,
                         IUpgradableSpecification upgradableSpecification,
                         ICustomFormatCalculationService formatCalculator,
                         IBroadcastSignalRMessage signalRBroadcaster)
        : base(episodeService, seriesService, upgradableSpecification, formatCalculator, signalRBroadcaster)
    {
    }

    [HttpGet]
    [Produces("application/json")]
    public List<EpisodeResource> GetEpisodes(int? seriesId, int? seasonNumber, [FromQuery]List<int> episodeIds, int? episodeFileId, [FromQuery] EpisodeSubresource[]? includeSubresources = null)
    {
        var includeSeries = includeSubresources.Contains(EpisodeSubresource.Series);
        var includeEpisodeFile = includeSubresources.Contains(EpisodeSubresource.EpisodeFile);
        var includeImages = includeSubresources.Contains(EpisodeSubresource.Images);

        if (seriesId.HasValue)
        {
            if (seasonNumber.HasValue)
            {
                return MapToResource(_episodeService.GetEpisodesBySeason(seriesId.Value, seasonNumber.Value), includeSeries, includeEpisodeFile, includeImages);
            }

            return MapToResource(_episodeService.GetEpisodeBySeries(seriesId.Value), includeSeries, includeEpisodeFile, includeImages);
        }
        else if (episodeIds.Any())
        {
            return MapToResource(_episodeService.GetEpisodes(episodeIds), includeSeries, includeEpisodeFile, includeImages);
        }
        else if (episodeFileId.HasValue)
        {
            return MapToResource(_episodeService.GetEpisodesByFileId(episodeFileId.Value), includeSeries, includeEpisodeFile, includeImages);
        }

        throw new BadRequestException("seriesId or episodeIds must be provided");
    }

    [RestPutById]
    [Consumes("application/json")]
    public ActionResult<EpisodeResource> SetEpisodeMonitored([FromRoute] int id, [FromBody] EpisodeResource resource)
    {
        _episodeService.SetEpisodeMonitored(id, resource.Monitored);

        resource = MapToResource(_episodeService.GetEpisode(id), false, false, false);

        return Accepted(resource);
    }

    [HttpPut("monitor")]
    [Consumes("application/json")]
    public IActionResult SetEpisodesMonitored([FromBody] EpisodesMonitoredResource resource, [FromQuery] EpisodeSubresource[]? includeSubresources = null)
    {
        var includeImages = includeSubresources.Contains(EpisodeSubresource.Images);

        if (resource.EpisodeIds.Count == 1)
        {
            _episodeService.SetEpisodeMonitored(resource.EpisodeIds.First(), resource.Monitored);
        }
        else
        {
            _episodeService.SetMonitored(resource.EpisodeIds, resource.Monitored);
        }

        var resources = MapToResource(_episodeService.GetEpisodes(resource.EpisodeIds), false, false, includeImages);

        return Accepted(resources);
    }
}
