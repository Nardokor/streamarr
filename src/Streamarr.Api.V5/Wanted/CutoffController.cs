using Microsoft.AspNetCore.Mvc;
using Streamarr.Api.V5.Episodes;
using Streamarr.Core.CustomFormats;
using Streamarr.Core.Datastore;
using Streamarr.Core.DecisionEngine.Specifications;
using Streamarr.Core.Tv;
using Streamarr.Http;
using Streamarr.Http.Extensions;
using Streamarr.SignalR;

namespace Streamarr.Api.V5.Wanted;

[V5ApiController("wanted/cutoff")]
public class CutoffController : EpisodeControllerWithSignalR
{
    private readonly IEpisodeCutoffService _episodeCutoffService;

    public CutoffController(IEpisodeCutoffService episodeCutoffService,
                        IEpisodeService episodeService,
                        ISeriesService seriesService,
                        IUpgradableSpecification upgradableSpecification,
                        ICustomFormatCalculationService formatCalculator,
                        IBroadcastSignalRMessage signalRBroadcaster)
        : base(episodeService, seriesService, upgradableSpecification, formatCalculator, signalRBroadcaster)
    {
        _episodeCutoffService = episodeCutoffService;
    }

    [HttpGet]
    [Produces("application/json")]
    public PagingResource<EpisodeResource> GetCutoffUnmetEpisodes([FromQuery] PagingRequestResource paging, bool monitored = true, [FromQuery] CutoffSubresource[]? includeSubresources = null)
    {
        var pagingResource = new PagingResource<EpisodeResource>(paging);
        var pagingSpec = pagingResource.MapToPagingSpec<EpisodeResource, Episode>(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "episodes.airDateUtc",
                "episodes.lastSearchTime",
                "series.sortTitle"
            },
            "episodes.airDateUtc",
            SortDirection.Ascending);

        if (monitored)
        {
            pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Series.Monitored == true);
        }
        else
        {
            pagingSpec.FilterExpressions.Add(v => v.Monitored == false || v.Series.Monitored == false);
        }

        var includeSeries = includeSubresources.Contains(CutoffSubresource.Series);
        var includeEpisodeFile = includeSubresources.Contains(CutoffSubresource.EpisodeFile);
        var includeImages = includeSubresources.Contains(CutoffSubresource.Images);

        var resource = pagingSpec.ApplyToPage(_episodeCutoffService.EpisodesWhereCutoffUnmet, v => MapToResource(v, includeSeries, includeEpisodeFile, includeImages));

        return resource;
    }
}
