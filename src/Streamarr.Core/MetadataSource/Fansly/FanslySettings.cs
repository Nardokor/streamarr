using FluentValidation;
using Streamarr.Core.Annotations;

namespace Streamarr.Core.MetadataSource.Fansly
{
    public class FanslySettingsValidator : AbstractValidator<MetadataSourceSettingsBase>
    {
        public FanslySettingsValidator()
        {
            RuleFor(c => ((FanslySettings)c).AuthToken)
                .NotEmpty()
                .WithMessage("An auth token is required to access Fansly content.");
        }
    }

    public class FanslySettings : MetadataSourceSettingsBase
    {
        private static readonly FanslySettingsValidator _validator = new FanslySettingsValidator();

        public FanslySettings()
        {
            // Fansly is a video/image platform; no live stream or shorts support.
            DefaultDownloadShorts = false;
            DefaultDownloadVods = false;
            DefaultDownloadLive = false;
        }

        [FieldDefinition(0, Label = "Auth Token", Type = FieldType.Password, HelpText = "Your Fansly session token. In your browser, open DevTools → Application → Local Storage → fansly.com and copy the value of 'session_active_session', then extract the 'token' field from the JSON.")]
        public string AuthToken { get; set; } = string.Empty;

        protected override AbstractValidator<MetadataSourceSettingsBase> Validator => _validator;
    }
}
