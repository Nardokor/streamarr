using Streamarr.Common.Messaging;
using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Messaging.Events
{
    public class CommandQueuedEvent : IEvent
    {
        public CommandModel Command { get; private set; }

        public CommandQueuedEvent(CommandModel command)
        {
            Command = command;
        }
    }
}
