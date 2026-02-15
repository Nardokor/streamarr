using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Parser
{
    public class InvalidDateException : StreamarrException
    {
        public InvalidDateException(string message, params object[] args)
            : base(message, args)
        {
        }

        public InvalidDateException(string message)
            : base(message)
        {
        }
    }
}
