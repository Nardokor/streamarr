using System.Collections.Generic;
using Streamarr.Common.Messaging;

namespace Streamarr.Core.Tv.Events
{
    public class SeriesImportedEvent : IEvent
    {
        public List<int> SeriesIds { get; private set; }

        public SeriesImportedEvent(List<int> seriesIds)
        {
            SeriesIds = seriesIds;
        }
    }
}
