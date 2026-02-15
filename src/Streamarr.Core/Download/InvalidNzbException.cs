using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Download
{
    public class InvalidNzbException : StreamarrException
    {
        public InvalidNzbException(string message, params object[] args)
            : base(message, args)
        {
        }

        public InvalidNzbException(string message)
            : base(message)
        {
        }

        public InvalidNzbException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }

        public InvalidNzbException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
