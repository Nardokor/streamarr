using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Download
{
    public class ProcessMonitoredDownloadsCommand : Command
    {
        public override bool RequiresDiskAccess => true;

        public override bool IsLongRunning => true;
    }
}
