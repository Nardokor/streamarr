using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Gotify
{
    public class GotifyException : StreamarrException
    {
        public GotifyException(string message)
            : base(message)
        {
        }

        public GotifyException(string message, params object[] args)
            : base(message, args)
        {
        }

        public GotifyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
