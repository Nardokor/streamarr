using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.Validation;

namespace Streamarr.Core.Notifications.Synology
{
    public class SynologyIndexerSettingsValidator : AbstractValidator<SynologyIndexerSettings>
    {
    }

    public class SynologyIndexerSettings : NotificationSettingsBase<SynologyIndexerSettings>
    {
        private static readonly SynologyIndexerSettingsValidator Validator = new();

        public SynologyIndexerSettings()
        {
            UpdateLibrary = true;
        }

        [FieldDefinition(0, Label = "NotificationsSettingsUpdateLibrary", Type = FieldType.Checkbox, HelpText = "NotificationsSynologySettingsUpdateLibraryHelpText")]
        public bool UpdateLibrary { get; set; }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
