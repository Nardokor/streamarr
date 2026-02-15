using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.Validation;

namespace Streamarr.Core.Notifications.Simplepush
{
    public class SimplepushSettingsValidator : AbstractValidator<SimplepushSettings>
    {
        public SimplepushSettingsValidator()
        {
            RuleFor(c => c.Key).NotEmpty();
        }
    }

    public class SimplepushSettings : NotificationSettingsBase<SimplepushSettings>
    {
        private static readonly SimplepushSettingsValidator Validator = new();

        [FieldDefinition(0, Label = "NotificationsSimplepushSettingsKey", Privacy = PrivacyLevel.ApiKey, HelpLink = "https://simplepush.io/features")]
        public string Key { get; set; }

        [FieldDefinition(1, Label = "NotificationsSimplepushSettingsEvent", HelpText = "NotificationsSimplepushSettingsEventHelpText", HelpLink = "https://simplepush.io/features")]
        public string Event { get; set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Key);

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
