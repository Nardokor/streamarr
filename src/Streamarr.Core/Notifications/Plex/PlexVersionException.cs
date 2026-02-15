using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Plex
{
    public class PlexVersionException : StreamarrException
    {
        public PlexVersionException(string message)
            : base(message)
        {
        }

        public PlexVersionException(string message, params object[] args)
            : base(message, args)
        {
        }
    }
}
