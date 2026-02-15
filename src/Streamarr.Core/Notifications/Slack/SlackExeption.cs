using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Slack
{
    public class SlackExeption : StreamarrException
    {
        public SlackExeption(string message)
            : base(message)
        {
        }

        public SlackExeption(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
