using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Notifications.Ntfy
{
    public class NtfyException : StreamarrException
    {
        public NtfyException(string message)
            : base(message)
        {
        }

        public NtfyException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
