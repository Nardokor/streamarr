using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Creators.Commands
{
    public class CheckLiveStreamsCommand : Command
    {
        public override bool SendUpdatesToClient => false;
        public override string CompletionMessage => "Live stream statuses updated";
    }
}
