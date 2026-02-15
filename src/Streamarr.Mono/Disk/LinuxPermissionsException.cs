using Streamarr.Common.Exceptions;

namespace Streamarr.Mono.Disk
{
    public class LinuxPermissionsException : StreamarrException
    {
        public LinuxPermissionsException(string message, params object[] args)
            : base(message, args)
        {
        }

        public LinuxPermissionsException(string message)
            : base(message)
        {
        }
    }
}
