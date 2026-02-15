using System;
using System.Net;
using Streamarr.Core.Exceptions;

namespace Streamarr.Core.MetadataSource.SkyHook
{
    public class SkyHookException : StreamarrClientException
    {
        public SkyHookException(string message)
            : base(HttpStatusCode.ServiceUnavailable, message)
        {
        }

        public SkyHookException(string message, params object[] args)
            : base(HttpStatusCode.ServiceUnavailable, message, args)
        {
        }

        public SkyHookException(string message, Exception innerException, params object[] args)
            : base(HttpStatusCode.ServiceUnavailable, message, innerException, args)
        {
        }
    }
}
