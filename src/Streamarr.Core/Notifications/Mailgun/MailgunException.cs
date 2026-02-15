using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Mailgun
{
    public class MailgunException : StreamarrException
    {
        public MailgunException(string message)
            : base(message)
        {
        }

        public MailgunException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
