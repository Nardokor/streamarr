using System.Net;
using Streamarr.Core.Exceptions;

namespace Streamarr.Core.Backup
{
    public class RestoreBackupFailedException : StreamarrClientException
    {
        public RestoreBackupFailedException(HttpStatusCode statusCode, string message, params object[] args)
            : base(statusCode, message, args)
        {
        }

        public RestoreBackupFailedException(HttpStatusCode statusCode, string message)
            : base(statusCode, message)
        {
        }
    }
}
