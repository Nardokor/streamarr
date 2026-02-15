using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Messaging.Commands
{
    public class CommandNotFoundException : StreamarrException
    {
        public CommandNotFoundException(string contract)
            : base("Couldn't find command " + contract)
        {
        }
    }
}
