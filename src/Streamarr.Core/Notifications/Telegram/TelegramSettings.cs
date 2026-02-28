using FluentValidation;
using Streamarr.Core.Annotations;

namespace Streamarr.Core.Notifications.Telegram
{
    public class TelegramSettingsValidator : AbstractValidator<TelegramSettings>
    {
        public TelegramSettingsValidator()
        {
            RuleFor(c => c.BotToken).NotEmpty().WithMessage("Bot token is required");
            RuleFor(c => c.ChatId).NotEmpty().WithMessage("Chat ID is required");
        }
    }

    public class TelegramSettings : NotificationSettingsBase<TelegramSettings>
    {
        private static readonly TelegramSettingsValidator _validator = new();

        [FieldDefinition(0, Label = "Bot Token", Privacy = PrivacyLevel.ApiKey)]
        public string BotToken { get; set; }

        [FieldDefinition(1, Label = "Chat ID", HelpText = "Your chat ID or channel username (e.g. @channelname)")]
        public string ChatId { get; set; }

        [FieldDefinition(2, Label = "Send Silently", Type = FieldType.Checkbox, HelpText = "Send notification without sound")]
        public bool SendSilently { get; set; }

        protected override AbstractValidator<TelegramSettings> Validator => _validator;
    }
}
