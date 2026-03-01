using FluentValidation;
using Streamarr.Core.Annotations;

namespace Streamarr.Core.MetadataSource.YouTube
{
    public class YouTubeSettingsValidator : AbstractValidator<MetadataSourceSettingsBase>
    {
        public YouTubeSettingsValidator()
        {
            // API key is optional — the source works without it, just with reduced metadata quality
        }
    }

    public class YouTubeSettings : MetadataSourceSettingsBase
    {
        private static readonly YouTubeSettingsValidator _validator = new();

        [FieldDefinition(0, Label = "API Key", Type = FieldType.Password, Privacy = PrivacyLevel.ApiKey, HelpText = "YouTube Data API v3 key from Google Cloud Console. Optional but strongly recommended.")]
        public string ApiKey { get; set; } = string.Empty;

        protected override AbstractValidator<MetadataSourceSettingsBase> Validator => _validator;
    }
}
