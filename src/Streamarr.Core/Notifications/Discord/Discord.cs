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

        public override void OnDownload(ContentDownloadedMessage message)
        {
            var username = !string.IsNullOrWhiteSpace(Settings.Username)
                ? Settings.Username
                : "Streamarr";

            var content = $"\u2b07\ufe0f Downloaded **{message.ContentTitle}** by {message.CreatorName} ({message.ChannelName})";
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
