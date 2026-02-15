using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Download.Clients
{
    public class DownloadClientException : StreamarrException
    {
        public DownloadClientException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }

        public DownloadClientException(string message)
            : base(message)
        {
        }

        public DownloadClientException(string message, Exception innerException, params object[] args)
            : base(string.Format(message, args), innerException)
        {
        }

        public DownloadClientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
