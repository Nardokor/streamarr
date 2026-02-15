using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Messaging.Commands
{
    public class CommandFailedException : StreamarrException
    {
        public CommandFailedException(string message, params object[] args)
            : base(message, args)
        {
        }

        public CommandFailedException(string message)
            : base(message)
        {
        }

        public CommandFailedException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }

        public CommandFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public CommandFailedException(Exception innerException)
            : base("Failed", innerException)
        {
        }
    }
}
