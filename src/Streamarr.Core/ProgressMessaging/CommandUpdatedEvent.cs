using Streamarr.Common.Messaging;
using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.ProgressMessaging
{
    public class CommandUpdatedEvent : IEvent
    {
        public CommandModel Command { get; set; }

        public CommandUpdatedEvent(CommandModel command)
        {
            Command = command;
        }
    }
}
