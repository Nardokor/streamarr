using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Download.YtDlp.Commands
{
    public class UpdateYtDlpCommand : Command
    {
        public override bool SendUpdatesToClient => true;
    }
}
