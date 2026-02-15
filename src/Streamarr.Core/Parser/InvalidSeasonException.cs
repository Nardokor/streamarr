using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Parser
{
    public class InvalidSeasonException : StreamarrException
    {
        public InvalidSeasonException(string message, params object[] args)
            : base(message, args)
        {
        }

        public InvalidSeasonException(string message)
            : base(message)
        {
        }
    }
}
