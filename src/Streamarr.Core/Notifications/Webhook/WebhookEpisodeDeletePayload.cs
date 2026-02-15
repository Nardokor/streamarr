using System.Collections.Generic;
using Streamarr.Core.MediaFiles;

namespace Streamarr.Core.Notifications.Webhook
{
    public class WebhookEpisodeDeletePayload : WebhookPayload
    {
        public WebhookSeries Series { get; set; }
        public List<WebhookEpisode> Episodes { get; set; }
        public WebhookEpisodeFile EpisodeFile { get; set; }
        public DeleteMediaFileReason DeleteReason { get; set; }
    }
}
