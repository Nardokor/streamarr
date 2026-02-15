using Streamarr.Common.Messaging;
using Streamarr.Core.Tv;

namespace Streamarr.Core.MediaCover
{
    public class MediaCoversUpdatedEvent : IEvent
    {
        public Series Series { get; set; }
        public bool Updated { get; set; }

        public MediaCoversUpdatedEvent(Series series, bool updated)
        {
            Series = series;
            Updated = updated;
        }
    }
}
