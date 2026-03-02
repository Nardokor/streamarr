using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentValidation.Results;
using NLog;

namespace Streamarr.Core.Notifications.Discord
{
    public class Discord : NotificationBase<DiscordSettings>
    {
        private static readonly HttpClient _http = new HttpClient();
        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly Logger _logger;

        public Discord(Logger logger)
        {
            _logger = logger;
        }

        public override string Name => "Discord";

        public override void OnGrab(ContentGrabbedMessage message)
        {
            var username = !string.IsNullOrWhiteSpace(Settings.Username)
                ? Settings.Username
                : "Streamarr";

            var content = $"\ud83d\udccc Queued **{message.ContentTitle}** by {message.CreatorName} ({message.ChannelName})";
            SendMessage(content, username);
        }

        public override void OnDownload(ContentDownloadedMessage message)
        {
            var username = !string.IsNullOrWhiteSpace(Settings.Username)
                ? Settings.Username
                : "Streamarr";

            var content = $"\u2b07\ufe0f Downloaded **{message.ContentTitle}** by {message.CreatorName} ({message.ChannelName})";
            SendMessage(content, username);
        }

        public override void OnLiveStreamStart(LiveStreamStartedMessage message)
        {
            var username = !string.IsNullOrWhiteSpace(Settings.Username)
                ? Settings.Username
                : "Streamarr";

            var content = $"\ud83d\udd34 Live stream started: **{message.ContentTitle}** on {message.ChannelName} by {message.CreatorName}";
            SendMessage(content, username);
        }

        public override void OnLiveStreamEnd(LiveStreamEndedMessage message)
        {
            var username = !string.IsNullOrWhiteSpace(Settings.Username)
                ? Settings.Username
                : "Streamarr";

            var content = $"\u23f9\ufe0f Live stream ended: **{message.ContentTitle}** by {message.CreatorName} ({message.ChannelName})";
            SendMessage(content, username);
        }

        public override void OnChannelAdded(ChannelAddedMessage message)
        {
            var username = !string.IsNullOrWhiteSpace(Settings.Username)
                ? Settings.Username
                : "Streamarr";

            var content = $"\u2795 Added channel **{message.ChannelTitle}** ({message.Platform}) by {message.CreatorName}";
            SendMessage(content, username);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                var username = !string.IsNullOrWhiteSpace(Settings.Username)
                    ? Settings.Username
                    : "Streamarr";

                SendMessage("Test message from Streamarr \u2705", username);
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex, "Unable to send test Discord notification");
                failures.Add(new ValidationFailure("WebHookUrl", $"Unable to send test notification: {ex.Message}"));
            }

            return new ValidationResult(failures);
        }

        private void SendMessage(string content, string username)
        {
            var payload = new { content, username };
            var json = JsonSerializer.Serialize(payload, _json);
            var body = new StringContent(json, Encoding.UTF8, "application/json");
            var response = _http.PostAsync(Settings.WebHookUrl, body).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }
    }
}
