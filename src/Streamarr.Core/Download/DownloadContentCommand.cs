using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Download
{
    public class DownloadContentCommand : Command
    {
        public int ContentId { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool IsLongRunning => true;
        public override string CompletionMessage => "Content downloaded";
    }
}
