using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Apprise
{
    public class AppriseException : StreamarrException
    {
        public AppriseException(string message)
            : base(message)
        {
        }

        public AppriseException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
