using System.Net;
using Streamarr.Core.Exceptions;

namespace Streamarr.Core.Download;

public class ResolveDownloadClientException : StreamarrClientException
{
    public ResolveDownloadClientException(string message, params object[] args)
        : base(HttpStatusCode.BadRequest, message, args)
    {
    }
}
