using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.SendGrid
{
    public class SendGridException : StreamarrException
    {
        public SendGridException(string message)
            : base(message)
        {
        }

        public SendGridException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
