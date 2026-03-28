using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.MetadataSource.YouTube
{
    public class RenewWebSubSubscriptionsCommand : Command
    {
        public override bool SendUpdatesToClient => false;
    }
}
