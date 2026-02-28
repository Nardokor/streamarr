using FluentValidation;
using Streamarr.Core.Annotations;

namespace Streamarr.Core.Notifications.Plex
{
    public class PlexServerSettingsValidator : AbstractValidator<PlexServerSettings>
    {
        public PlexServerSettingsValidator()
        {
            RuleFor(c => c.Host).NotEmpty().WithMessage("Host is required");
            RuleFor(c => c.Port).InclusiveBetween(1, 65535).WithMessage("Port must be between 1 and 65535");
            RuleFor(c => c.AuthToken).NotEmpty().WithMessage("Auth token is required");
        }
    }

    public class PlexServerSettings : NotificationSettingsBase<PlexServerSettings>
    {
        private static readonly PlexServerSettingsValidator _validator = new();

        public PlexServerSettings()
        {
            Port = 32400;
        }

        [FieldDefinition(0, Label = "Host")]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Number)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "Use SSL", Type = FieldType.Checkbox)]
        public bool UseSsl { get; set; }

        [FieldDefinition(3, Label = "Auth Token", Privacy = PrivacyLevel.ApiKey, HelpText = "Plex authentication token")]
        public string AuthToken { get; set; }

        protected override AbstractValidator<PlexServerSettings> Validator => _validator;
    }
}
