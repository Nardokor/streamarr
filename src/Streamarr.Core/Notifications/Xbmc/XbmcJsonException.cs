using System;

namespace Streamarr.Core.Notifications.Xbmc
{
    public class XbmcJsonException : Exception
    {
        public XbmcJsonException()
        {
        }

        public XbmcJsonException(string message)
            : base(message)
        {
        }
    }
}
