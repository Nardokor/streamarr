using Microsoft.AspNetCore.Mvc;
using Streamarr.Api.V1.Episodes;
using Streamarr.Common.Extensions;
using Streamarr.Core.CustomFormats;
using Streamarr.Core.DecisionEngine.Specifications;
using Streamarr.Core.Tags;
using Streamarr.Core.Tv;
using Streamarr.Http;
using Streamarr.SignalR;

namespace Streamarr.Api.V1.Calendar
{
    [V1ApiController]
    public class CalendarController : EpisodeControllerWithSignalR
    {
        private readonly ITagService _tagService;

        public CalendarController(IBroadcastSignalRMessage signalR,
                            IEpisodeService episodeService,
                            ISeriesService seriesService,
                            IUpgradableSpecification qualityUpgradableSpecification,
                            ITagService tagService,
                            ICustomFormatCalculationService formatCalculator)
            : base(episodeService, seriesService, qualityUpgradableSpecification, formatCalculator, signalR)
        {
            _tagService = tagService;
        }

        [HttpGet]
        [Produces("application/json")]
        public List<EpisodeResource> GetCalendar(DateTime? start, DateTime? end, bool includeUnmonitored = false, bool includeSpecials = true, string tags = "", [FromQuery] CalendarSubresource[]? includeSubresources = null)
        {
            var startUse = start ?? DateTime.Today;
            var endUse = end ?? DateTime.Today.AddDays(2);
            var episodes = _episodeService.EpisodesBetweenDates(startUse, endUse, includeUnmonitored, includeSpecials);
            var allSeries = _seriesService.GetAllSeries();
            var parsedTags = new List<int>();
            var result = new List<Episode>();

            if (tags.IsNotNullOrWhiteSpace())
            {
                parsedTags.AddRange(tags.Split(',').Select(_tagService.GetTag).Select(t => t.Id));
            }

            foreach (var episode in episodes)
            {
                var series = allSeries.SingleOrDefault(s => s.Id == episode.SeriesId);

                if (series == null)
                {
                    continue;
                }

                if (parsedTags.Any() && parsedTags.None(series.Tags.Contains))
                {
                    continue;
                }

                result.Add(episode);
            }

            var includeSeries = includeSubresources.Contains(CalendarSubresource.Series);
            var includeEpisodeFile = includeSubresources.Contains(CalendarSubresource.EpisodeFile);
            var includeEpisodeImages = includeSubresources.Contains(CalendarSubresource.Images);

            var resources = MapToResource(result, includeSeries, includeEpisodeFile, includeEpisodeImages);

            return resources.OrderBy(e => e.AirDateUtc).ToList();
        }
    }
}
