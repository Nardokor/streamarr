using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Update.Commands
{
    public class ApplicationUpdateCheckCommand : Command
    {
        public override bool SendUpdatesToClient => true;

        public override string CompletionMessage => null;

        public bool InstallMajorUpdate { get; set; }
    }
}
