using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Configuration
{
    public class ResetApiKeyCommand : Command
    {
        public override bool SendUpdatesToClient => true;
    }
}
