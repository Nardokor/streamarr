using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Indexers.Torznab
{
    public class TorznabException : StreamarrException
    {
        public TorznabException(string message, params object[] args)
            : base(message, args)
        {
        }

        public TorznabException(string message)
            : base(message)
        {
        }
    }
}
