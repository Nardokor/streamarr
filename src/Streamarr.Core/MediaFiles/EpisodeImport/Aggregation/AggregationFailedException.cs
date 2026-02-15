using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.MediaFiles.EpisodeImport.Aggregation
{
    public class AugmentingFailedException : StreamarrException
    {
        public AugmentingFailedException(string message, params object[] args)
            : base(message, args)
        {
        }

        public AugmentingFailedException(string message)
            : base(message)
        {
        }

        public AugmentingFailedException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }

        public AugmentingFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
