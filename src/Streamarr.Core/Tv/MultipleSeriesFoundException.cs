using System.Collections.Generic;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Tv
{
    public class MultipleSeriesFoundException : StreamarrException
    {
        public List<Series> Series { get; set; }

        public MultipleSeriesFoundException(List<Series> series, string message, params object[] args)
            : base(message, args)
        {
            Series = series;
        }
    }
}
