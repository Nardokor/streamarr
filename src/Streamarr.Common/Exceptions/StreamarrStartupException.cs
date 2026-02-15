using System;

namespace Streamarr.Common.Exceptions
{
    public class StreamarrStartupException : StreamarrException
    {
        public StreamarrStartupException(string message, params object[] args)
            : base("Streamarr failed to start: " + string.Format(message, args))
        {
        }

        public StreamarrStartupException(string message)
            : base("Streamarr failed to start: " + message)
        {
        }

        public StreamarrStartupException()
            : base("Streamarr failed to start")
        {
        }

        public StreamarrStartupException(Exception innerException, string message, params object[] args)
            : base("Streamarr failed to start: " + string.Format(message, args), innerException)
        {
        }

        public StreamarrStartupException(Exception innerException, string message)
            : base("Streamarr failed to start: " + message, innerException)
        {
        }

        public StreamarrStartupException(Exception innerException)
            : base("Streamarr failed to start: " + innerException.Message)
        {
        }
    }
}
