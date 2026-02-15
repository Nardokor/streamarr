using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Webhook
{
    public class WebhookException : StreamarrException
    {
        public WebhookException(string message)
            : base(message)
        {
        }

        public WebhookException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
