using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Creators.Commands
{
    public class DownloadMissingContentCommand : Command
    {
        public int? CreatorId { get; set; }
        public int? ChannelId { get; set; }

        public override bool SendUpdatesToClient => true;

        public override string CompletionMessage => "Download queued";
    }
}
