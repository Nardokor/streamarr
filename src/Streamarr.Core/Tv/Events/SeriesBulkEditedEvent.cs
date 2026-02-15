using System.Collections.Generic;
using Streamarr.Common.Messaging;

namespace Streamarr.Core.Tv.Events
{
    public class SeriesBulkEditedEvent : IEvent
    {
        public List<Series> Series { get; private set; }

        public SeriesBulkEditedEvent(List<Series> series)
        {
            Series = series;
        }
    }
}
