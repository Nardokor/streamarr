using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Notifiarr
{
    public class NotifiarrException : StreamarrException
    {
        public NotifiarrException(string message)
            : base(message)
        {
        }

        public NotifiarrException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
