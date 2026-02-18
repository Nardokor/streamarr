using Streamarr.Common.Messaging;

namespace Streamarr.Core.Creators.Events
{
    public class CreatorAddedEvent : IEvent
    {
        public Creator Creator { get; }

        public CreatorAddedEvent(Creator creator)
        {
            Creator = creator;
        }
    }
}
