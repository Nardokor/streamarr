using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Trakt
{
    public class TraktException : StreamarrException
    {
        public TraktException(string message)
            : base(message)
        {
        }

        public TraktException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
