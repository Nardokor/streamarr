using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Content.Commands
{
    public class RetentionCleanupCommand : Command
    {
        public override string CompletionMessage => "Retention cleanup complete";
    }
}
