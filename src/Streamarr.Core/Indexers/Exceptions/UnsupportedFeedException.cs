using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Indexers.Exceptions
{
    public class UnsupportedFeedException : StreamarrException
    {
        public UnsupportedFeedException(string message, params object[] args)
            : base(message, args)
        {
        }

        public UnsupportedFeedException(string message)
            : base(message)
        {
        }
    }
}
