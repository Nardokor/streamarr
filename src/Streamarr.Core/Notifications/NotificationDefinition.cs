using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.Notifications
{
    public class NotificationDefinition : ProviderDefinition
    {
        public bool OnDownload { get; set; }
        public bool SupportsOnDownload { get; set; }
    }
}
