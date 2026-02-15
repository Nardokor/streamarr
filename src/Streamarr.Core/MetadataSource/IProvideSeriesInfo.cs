using System;
using System.Collections.Generic;
using Streamarr.Core.Tv;

namespace Streamarr.Core.MetadataSource
{
    public interface IProvideSeriesInfo
    {
        Tuple<Series, List<Episode>> GetSeriesInfo(int tvdbSeriesId);
    }
}
