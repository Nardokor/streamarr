using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Exceptions
{
    public class SeriesNotFoundException : StreamarrException
    {
        public int TvdbSeriesId { get; set; }

        public SeriesNotFoundException(int tvdbSeriesId)
            : base(string.Format("Series with tvdbid {0} was not found, it may have been removed from TheTVDB.", tvdbSeriesId))
        {
            TvdbSeriesId = tvdbSeriesId;
        }

        public SeriesNotFoundException(int tvdbSeriesId, string message, params object[] args)
            : base(message, args)
        {
            TvdbSeriesId = tvdbSeriesId;
        }

        public SeriesNotFoundException(int tvdbSeriesId, string message)
            : base(message)
        {
            TvdbSeriesId = tvdbSeriesId;
        }
    }
}
