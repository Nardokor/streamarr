using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Content.Commands
{
    public class CheckMirroredContentCommand : Command
    {
        public override string CompletionMessage => "Mirror availability check complete";
    }
}
