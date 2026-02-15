using Streamarr.Common.Exceptions;

namespace Streamarr.Common.Disk
{
    public class NotParentException : StreamarrException
    {
        public NotParentException(string message, params object[] args)
            : base(message, args)
        {
        }

        public NotParentException(string message)
            : base(message)
        {
        }
    }
}
