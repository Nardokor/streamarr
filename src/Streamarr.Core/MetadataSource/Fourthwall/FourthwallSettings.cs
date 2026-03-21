using FluentValidation;
using Streamarr.Core.Annotations;

namespace Streamarr.Core.MetadataSource.Fourthwall
{
    public class FourthwallSettingsValidator : AbstractValidator<MetadataSourceSettingsBase>
    {
        public FourthwallSettingsValidator()
        {
            RuleFor(c => ((FourthwallSettings)c).CookiesFilePath)
                .NotEmpty()
                .WithMessage("A cookies file is required to access Fourthwall member content.");
        }
    }

    public class FourthwallSettings : MetadataSourceSettingsBase
    {
        private static readonly FourthwallSettingsValidator _validator = new FourthwallSettingsValidator();

        public FourthwallSettings()
        {
            // Fourthwall channels have videos and live streams (unlisted YouTube).
            // No shorts or VODs.
            DefaultDownloadShorts = false;
            DefaultDownloadVods = false;
            DefaultDownloadLive = true;
        }

        [FieldDefinition(0, Label = "Cookies File", Type = FieldType.FilePath, HelpText = "Path to a Netscape-format cookies.txt file exported from your browser while logged in to Fourthwall. Required to access member content.")]
        public string CookiesFilePath { get; set; } = string.Empty;

        [FieldDefinition(1, Label = "Use YouTube API for Live Detection", Type = FieldType.Checkbox, HelpText = "Use the YouTube Data API to detect upcoming and live streams in this Fourthwall feed. Requires a YouTube source with an API key configured. When enabled, streams posted to Fourthwall before they go live are detected as Upcoming and recorded automatically when they start.")]
        public bool UseYouTubeApi { get; set; } = true;

        protected override AbstractValidator<MetadataSourceSettingsBase> Validator => _validator;
    }
}
