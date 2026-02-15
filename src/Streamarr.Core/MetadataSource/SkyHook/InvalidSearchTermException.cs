using System.Net;
using Streamarr.Core.Exceptions;

namespace Streamarr.Core.MetadataSource.SkyHook;

public class InvalidSearchTermException : StreamarrClientException
{
    public InvalidSearchTermException(string message, params object[] args)
        : base(HttpStatusCode.BadRequest, message, args)
    {
    }
}
