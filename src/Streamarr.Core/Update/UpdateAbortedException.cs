using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Update
{
    public class UpdateFailedException : StreamarrException
    {
        public UpdateFailedException(string message, params object[] args)
            : base(message, args)
        {
        }

        public UpdateFailedException(string message)
            : base(message)
        {
        }
    }
}
