using System;
using System.Net;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Exceptions
{
    public class StreamarrClientException : StreamarrException
    {
        public HttpStatusCode StatusCode { get; private set; }

        public StreamarrClientException(HttpStatusCode statusCode, string message, params object[] args)
            : base(message, args)
        {
            StatusCode = statusCode;
        }

        public StreamarrClientException(HttpStatusCode statusCode, string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
            StatusCode = statusCode;
        }

        public StreamarrClientException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
