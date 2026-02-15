using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Exceptions
{
    public class SearchFailedException : StreamarrException
    {
        public SearchFailedException(string message)
            : base(message)
        {
        }
    }
}
