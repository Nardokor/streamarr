using System.Net;
using Streamarr.Core.Exceptions;

namespace Streamarr.Core.Indexers;

public class ResolveIndexerException : StreamarrClientException
{
    public ResolveIndexerException(string message, params object[] args)
        : base(HttpStatusCode.BadRequest, message, args)
    {
    }
}
