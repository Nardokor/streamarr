using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Join
{
    public class JoinException : StreamarrException
    {
        public JoinException(string message)
            : base(message)
        {
        }

        public JoinException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
