using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentValidation.Results;
using NLog;

namespace Streamarr.Core.Notifications.Telegram
{
    public class Telegram : NotificationBase<TelegramSettings>
    {
        private static readonly HttpClient _http = new HttpClient();
        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly Logger _logger;

        public Telegram(Logger logger)
        {
            _logger = logger;
        }

        public override string Name => "Telegram";

        public override void OnGrab(ContentGrabbedMessage message)
        {
            var text = $"\ud83d\udccc Queued *{Escape(message.ContentTitle)}* by {Escape(message.CreatorName)} \\({Escape(message.ChannelName)}\\)";
            SendMessage(text);
        }

        public override void OnDownload(ContentDownloadedMessage message)
        {
            var text = $"\u2b07\ufe0f Downloaded *{Escape(message.ContentTitle)}* by {Escape(message.CreatorName)} \\({Escape(message.ChannelName)}\\)";
            SendMessage(text);
        }

        public override void OnLiveStreamStart(LiveStreamStartedMessage message)
        {
            var text = $"\ud83d\udd34 Live stream started: *{Escape(message.ContentTitle)}* on {Escape(message.ChannelName)} by {Escape(message.CreatorName)}";
            SendMessage(text);
        }

        public override void OnLiveStreamEnd(LiveStreamEndedMessage message)
        {
            var text = $"\u23f9\ufe0f Live stream ended: *{Escape(message.ContentTitle)}* by {Escape(message.CreatorName)} \\({Escape(message.ChannelName)}\\)";
            SendMessage(text);
        }

        public override void OnChannelAdded(ChannelAddedMessage message)
        {
            var text = $"\u2795 Added channel *{Escape(message.ChannelTitle)}* \\({message.Platform}\\) by {Escape(message.CreatorName)}";
            SendMessage(text);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                SendMessage("Test message from Streamarr \u2705");
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex, "Unable to send test Telegram notification");
                failures.Add(new ValidationFailure("BotToken", $"Unable to send test notification: {ex.Message}"));
            }

            return new ValidationResult(failures);
        }

        private void SendMessage(string text)
        {
            var url = $"https://api.telegram.org/bot{Settings.BotToken}/sendMessage";
            var payload = new
            {
                chat_id = Settings.ChatId,
                text,
                parse_mode = "MarkdownV2",
                disable_notification = Settings.SendSilently
            };
            var json = JsonSerializer.Serialize(payload, _json);
            var body = new StringContent(json, Encoding.UTF8, "application/json");
            var response = _http.PostAsync(url, body).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return text
                .Replace("_", "\\_")
                .Replace("*", "\\*")
                .Replace("[", "\\[")
                .Replace("]", "\\]")
                .Replace("~", "\\~")
                .Replace("`", "\\`")
                .Replace(">", "\\>")
                .Replace("#", "\\#")
                .Replace("+", "\\+")
                .Replace("-", "\\-")
                .Replace("=", "\\=")
                .Replace("|", "\\|")
                .Replace("{", "\\{")
                .Replace("}", "\\}")
                .Replace(".", "\\.")
                .Replace("!", "\\!");
        }
    }
}
