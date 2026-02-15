using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Indexers.Exceptions
{
    public class ApiKeyException : StreamarrException
    {
        public ApiKeyException(string message, params object[] args)
            : base(message, args)
        {
        }

        public ApiKeyException(string message)
            : base(message)
        {
        }
    }
}
