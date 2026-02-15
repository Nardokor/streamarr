using System;

namespace Streamarr.Common.Exceptions
{
    public abstract class StreamarrException : ApplicationException
    {
        protected StreamarrException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }

        protected StreamarrException(string message)
            : base(message)
        {
        }

        protected StreamarrException(string message, Exception innerException, params object[] args)
            : base(string.Format(message, args), innerException)
        {
        }

        protected StreamarrException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
