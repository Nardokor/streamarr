using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Synology
{
    public class SynologyException : StreamarrException
    {
        public SynologyException(string message)
            : base(message)
        {
        }

        public SynologyException(string message, params object[] args)
            : base(message, args)
        {
        }
    }
}
