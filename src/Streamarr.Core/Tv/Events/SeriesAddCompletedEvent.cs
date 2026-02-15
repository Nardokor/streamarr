using Streamarr.Common.Messaging;

namespace Streamarr.Core.Tv.Events
{
    public class SeriesAddCompletedEvent : IEvent
    {
        public Series Series { get; private set; }

        public SeriesAddCompletedEvent(Series series)
        {
            Series = series;
        }
    }
}
