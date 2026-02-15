using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Instrumentation.Commands
{
    public class ClearLogCommand : Command
    {
        public override bool SendUpdatesToClient => true;
    }
}
