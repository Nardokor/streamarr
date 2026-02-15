using Streamarr.Common.Messaging;
using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Messaging.Events
{
    public class CommandExecutedEvent : IEvent
    {
        public CommandModel Command { get; private set; }

        public CommandExecutedEvent(CommandModel command)
        {
            Command = command;
        }
    }
}
