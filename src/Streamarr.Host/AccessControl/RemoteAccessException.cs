using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Host.AccessControl
{
    public class RemoteAccessException : StreamarrException
    {
        public RemoteAccessException(string message, params object[] args)
            : base(message, args)
        {
        }

        public RemoteAccessException(string message)
            : base(message)
        {
        }

        public RemoteAccessException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }

        public RemoteAccessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
