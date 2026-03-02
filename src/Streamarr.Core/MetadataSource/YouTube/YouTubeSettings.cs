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

        // YouTube-only content types (Twitch has no Videos or Shorts)
        [FieldDefinition(100, Label = "Download Videos", Type = FieldType.Checkbox, HelpText = "Download regular video uploads by default for new channels.")]
        public bool DefaultDownloadVideos { get; set; }

        [FieldDefinition(101, Label = "Download Shorts", Type = FieldType.Checkbox, HelpText = "Download short-form content by default for new channels.")]
        public bool DefaultDownloadShorts { get; set; }

        [FieldDefinition(111, Label = "Retention: Videos", Type = FieldType.Checkbox, HelpText = "Apply retention to Videos by default for new channels.")]
        public bool DefaultRetentionVideos { get; set; }

        [FieldDefinition(112, Label = "Retention: Shorts", Type = FieldType.Checkbox, HelpText = "Apply retention to Shorts by default for new channels.")]
        public bool DefaultRetentionShorts { get; set; }

        protected override AbstractValidator<MetadataSourceSettingsBase> Validator => _validator;
    }
}
