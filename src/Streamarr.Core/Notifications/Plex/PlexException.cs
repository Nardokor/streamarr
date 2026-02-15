using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Plex
{
    public class PlexException : StreamarrException
    {
        public PlexException(string message)
            : base(message)
        {
        }

        public PlexException(string message, params object[] args)
            : base(message, args)
        {
        }

        public PlexException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
