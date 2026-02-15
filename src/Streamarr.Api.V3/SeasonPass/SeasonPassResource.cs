using System.Collections.Generic;
using Streamarr.Core.Tv;

namespace Streamarr.Api.V3.SeasonPass
{
    public class SeasonPassResource
    {
        public List<SeasonPassSeriesResource> Series { get; set; }
        public MonitoringOptions MonitoringOptions { get; set; }
    }
}
