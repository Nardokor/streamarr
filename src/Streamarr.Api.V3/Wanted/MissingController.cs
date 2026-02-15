using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.CustomFormats;
using Streamarr.Core.Datastore;
using Streamarr.Core.DecisionEngine.Specifications;
using Streamarr.Core.Tv;
using Streamarr.SignalR;
using Streamarr.Api.V3.Episodes;
using Streamarr.Http;
using Streamarr.Http.Extensions;

namespace Streamarr.Api.V3.Wanted
{
    [V3ApiController("wanted/missing")]
    public class MissingController : EpisodeControllerWithSignalR
    {
        public MissingController(IEpisodeService episodeService,
                             ISeriesService seriesService,
                             IUpgradableSpecification upgradableSpecification,
                             ICustomFormatCalculationService formatCalculator,
                             IBroadcastSignalRMessage signalRBroadcaster)
            : base(episodeService, seriesService, upgradableSpecification, formatCalculator, signalRBroadcaster)
        {
        }

        [HttpGet]
        [Produces("application/json")]
        public PagingResource<EpisodeResource> GetMissingEpisodes([FromQuery] PagingRequestResource paging, bool includeSeries = false, bool includeImages = false, bool monitored = true)
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

            var resource = pagingSpec.ApplyToPage(_episodeService.EpisodesWithoutFiles, v => MapToResource(v, includeSeries, false, includeImages));

            return resource;
        }
    }
}
