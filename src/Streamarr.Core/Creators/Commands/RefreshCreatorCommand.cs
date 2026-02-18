using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Creators.Commands
{
    public class RefreshCreatorCommand : Command
    {
        public int? CreatorId { get; set; }

        public override bool SendUpdatesToClient => true;

        public override string CompletionMessage => "Completed";
    }
}
