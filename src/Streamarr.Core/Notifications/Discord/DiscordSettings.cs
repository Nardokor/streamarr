using FluentValidation;
using Streamarr.Core.Annotations;

namespace Streamarr.Core.Notifications.Discord
{
    public class DiscordSettingsValidator : AbstractValidator<DiscordSettings>
    {
        public DiscordSettingsValidator()
        {
            RuleFor(c => c.WebHookUrl).NotEmpty().WithMessage("Webhook URL is required");
            RuleFor(c => c.WebHookUrl).Must(u => u.StartsWith("https://discord.com/api/webhooks/") ||
                                                  u.StartsWith("https://discordapp.com/api/webhooks/"))
                .WithMessage("Must be a valid Discord webhook URL")
                .When(c => !string.IsNullOrWhiteSpace(c.WebHookUrl));
        }
    }

    public class DiscordSettings : NotificationSettingsBase<DiscordSettings>
    {
        private static readonly DiscordSettingsValidator _validator = new();

        [FieldDefinition(0, Label = "Webhook URL")]
        public string WebHookUrl { get; set; }

        [FieldDefinition(1, Label = "Username", HelpText = "Override the default username of the webhook")]
        public string Username { get; set; }

        protected override AbstractValidator<DiscordSettings> Validator => _validator;
    }
}
