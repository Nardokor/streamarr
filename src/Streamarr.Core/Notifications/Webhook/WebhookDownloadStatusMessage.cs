using System.Collections.Generic;
using System.Linq;
using Streamarr.Core.Download.TrackedDownloads;

namespace Streamarr.Core.Notifications.Webhook
{
    public class WebhookDownloadStatusMessage
    {
        public string Title { get; set; }
        public List<string> Messages { get; set; }

        public WebhookDownloadStatusMessage(TrackedDownloadStatusMessage statusMessage)
        {
            Title = statusMessage.Title;
            Messages = statusMessage.Messages.ToList();
        }
    }
}
