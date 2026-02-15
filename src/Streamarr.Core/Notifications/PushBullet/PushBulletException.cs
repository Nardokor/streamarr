using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.PushBullet
{
    public class PushBulletException : StreamarrException
    {
        public PushBulletException(string message)
            : base(message)
        {
        }

        public PushBulletException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
