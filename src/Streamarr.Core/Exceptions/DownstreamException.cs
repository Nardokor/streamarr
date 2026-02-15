using System.Net;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Exceptions
{
    public class DownstreamException : StreamarrException
    {
        public HttpStatusCode StatusCode { get; private set; }

        public DownstreamException(HttpStatusCode statusCode, string message, params object[] args)
            : base(message, args)
        {
            StatusCode = statusCode;
        }

        public DownstreamException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
