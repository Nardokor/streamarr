using System.Text.Json.Serialization;
using Streamarr.Core.CustomFormats;

namespace Streamarr.Core.Notifications.Webhook
{
    public class WebhookCustomFormat
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int Id { get; set; }
        public string Name { get; set; }

        public WebhookCustomFormat(CustomFormat customFormat)
        {
            Id = customFormat.Id;
            Name = customFormat.Name;
        }
    }
}
