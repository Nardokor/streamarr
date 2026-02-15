using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Organizer
{
    public class NamingFormatException : StreamarrException
    {
        public NamingFormatException(string message, params object[] args)
            : base(message, args)
        {
        }

        public NamingFormatException(string message)
            : base(message)
        {
        }
    }
}
