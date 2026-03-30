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
            // Fansly has no shorts or VODs; live defaults off until the user opts in.
            DefaultDownloadShorts = false;
            DefaultDownloadVods = false;
            DefaultDownloadLive = false;
            DefaultKeepVideos = true;
        }

        [FieldDefinition(0, Label = "Auth Token", Type = FieldType.Password, HelpText = "Your Fansly session token. In your browser, open DevTools → Application → Local Storage → fansly.com and copy the value of 'session_active_session', then extract the 'token' field from the JSON.")]
        public string AuthToken { get; set; } = string.Empty;

        // Fansly posts are the primary content type — use "Posts" label instead of "Videos".
        [FieldDefinition(100, Label = "Download Posts", Type = FieldType.Checkbox, HelpText = "Download posts by default for new channels.")]
        public override bool DefaultDownloadVideos { get; set; } = true;

        // ── Fields hidden from the generic form ────────────────────────────────
        // Fansly has no shorts, no VODs, and no word-based filters.
        // Override the virtual base properties WITHOUT [FieldDefinition] so SchemaBuilder
        // excludes them from the fields array (they still function normally in code).

        public override bool DefaultDownloadShorts { get; set; }
        public override bool DefaultDownloadVods { get; set; }
        public override string DefaultWatchedWords { get; set; } = string.Empty;
        public override string DefaultIgnoredWords { get; set; } = string.Empty;
        public override bool DefaultWatchedDefeatsIgnored { get; set; } = true;
        public override bool DefaultKeepShorts { get; set; }
        public override bool DefaultKeepVods { get; set; }
        public override string DefaultRetentionKeepWords { get; set; } = string.Empty;

        protected override AbstractValidator<MetadataSourceSettingsBase> Validator => _validator;
    }
}
