using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Configuration
{
    public class InvalidConfigFileException : StreamarrException
    {
        public InvalidConfigFileException(string message)
            : base(message)
        {
        }

        public InvalidConfigFileException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
