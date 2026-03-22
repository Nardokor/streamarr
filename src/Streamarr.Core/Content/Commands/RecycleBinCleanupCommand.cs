using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Content.Commands
{
    public class RecycleBinCleanupCommand : Command
    {
        public override string CompletionMessage => "Recycle bin cleanup complete";
    }
}
