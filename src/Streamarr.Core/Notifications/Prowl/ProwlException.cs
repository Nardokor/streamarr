using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Prowl
{
    public class ProwlException : StreamarrException
    {
        public ProwlException(string message)
            : base(message)
        {
        }

        public ProwlException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
