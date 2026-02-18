using Streamarr.Common.Messaging;

namespace Streamarr.Core.Creators.Events
{
    public class CreatorUpdatedEvent : IEvent
    {
        public Creator Creator { get; }

        public CreatorUpdatedEvent(Creator creator)
        {
            Creator = creator;
        }
    }
}
