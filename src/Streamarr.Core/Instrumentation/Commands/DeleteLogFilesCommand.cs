using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Instrumentation.Commands
{
    public class DeleteLogFilesCommand : Command
    {
        public override bool SendUpdatesToClient => true;
    }
}
