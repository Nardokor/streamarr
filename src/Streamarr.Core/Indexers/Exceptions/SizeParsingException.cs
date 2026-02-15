using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Indexers.Exceptions
{
    public class SizeParsingException : StreamarrException
    {
        public SizeParsingException(string message, params object[] args)
            : base(message, args)
        {
        }
    }
}
