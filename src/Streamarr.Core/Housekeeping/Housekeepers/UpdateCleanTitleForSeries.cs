using System.Linq;
using Streamarr.Core.Parser;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Housekeeping.Housekeepers
{
    public class UpdateCleanTitleForSeries : IHousekeepingTask
    {
        private readonly ISeriesRepository _seriesRepository;

        public UpdateCleanTitleForSeries(ISeriesRepository seriesRepository)
        {
            _seriesRepository = seriesRepository;
        }

        public void Clean()
        {
            var series = _seriesRepository.All().ToList();

            series.ForEach(s =>
            {
                var cleanTitle = s.Title.CleanSeriesTitle();
                if (s.CleanTitle != cleanTitle)
                {
                    s.CleanTitle = cleanTitle;
                    _seriesRepository.Update(s);
                }
            });
        }
    }
}
