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

        [FieldDefinition(1, Label = "Webhook Base URL", HelpText = "Public base URL for push notifications via Tailscale Funnel (e.g. https://streamarr.your-tailnet.ts.net). Streamarr will append /api/v1/webhook/youtube. Leave empty to use polling only.")]
        public string WebhookBaseUrl { get; set; } = string.Empty;

        [FieldDefinition(2, Label = "Cookies File", Type = FieldType.FilePath, HelpText = "Path to a Netscape-format cookies.txt file exported from your browser while logged in to YouTube. Required to access members-only content.")]
        public string CookiesFilePath { get; set; } = string.Empty;

        protected override AbstractValidator<MetadataSourceSettingsBase> Validator => _validator;
    }
}
