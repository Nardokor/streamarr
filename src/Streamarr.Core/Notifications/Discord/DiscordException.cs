using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Discord
{
    public class DiscordException : StreamarrException
    {
        public DiscordException(string message)
            : base(message)
        {
        }

        public DiscordException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
