using Streamarr.Common.Messaging;

namespace Streamarr.Core.Creators.Events
{
    public class CreatorDeletedEvent : IEvent
    {
        public Creator Creator { get; }

        public CreatorDeletedEvent(Creator creator)
        {
            Creator = creator;
        }
    }
}
