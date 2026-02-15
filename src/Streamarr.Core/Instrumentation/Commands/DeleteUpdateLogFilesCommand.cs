using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Instrumentation.Commands
{
    public class DeleteUpdateLogFilesCommand : Command
    {
        public override bool SendUpdatesToClient => true;
    }
}
