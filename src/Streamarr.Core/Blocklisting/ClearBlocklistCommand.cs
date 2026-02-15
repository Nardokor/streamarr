using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Blocklisting
{
    public class ClearBlocklistCommand : Command
    {
        public override bool SendUpdatesToClient => true;
    }
}
