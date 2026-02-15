using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.MediaFiles
{
    public class ScriptImportException : StreamarrException
    {
        public ScriptImportException(string message)
            : base(message)
        {
        }

        public ScriptImportException(string message, params object[] args)
            : base(message, args)
        {
        }

        public ScriptImportException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
