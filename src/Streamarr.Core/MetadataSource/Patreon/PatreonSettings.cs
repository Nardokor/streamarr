using FluentValidation;
using Streamarr.Core.Annotations;

namespace Streamarr.Core.MetadataSource.Patreon
{
    public class PatreonSettingsValidator : AbstractValidator<MetadataSourceSettingsBase>
    {
        public PatreonSettingsValidator()
        {
            RuleFor(c => ((PatreonSettings)c).CookiesFilePath)
                .NotEmpty()
                .WithMessage("A cookies file is required to access Patreon member content.");
        }
    }

    public class PatreonSettings : MetadataSourceSettingsBase
    {
        private static readonly PatreonSettingsValidator _validator = new PatreonSettingsValidator();

        public PatreonSettings()
        {
            // Patreon is a video-and-post platform; no live streams or shorts.
            DefaultDownloadShorts = false;
            DefaultDownloadVods = false;
            DefaultDownloadLive = false;
        }

        [FieldDefinition(0, Label = "Cookies File", Type = FieldType.FilePath, HelpText = "Path to a Netscape-format cookies.txt file exported from your browser while logged in to Patreon. Required to access patron-only content.")]
        public string CookiesFilePath { get; set; } = string.Empty;

        // Override without [FieldDefinition] so SchemaBuilder excludes these from the fields array.
        // Patreon has no shorts, VODs, or live streams.
        public override bool DefaultDownloadShorts { get; set; }
        public override bool DefaultDownloadVods { get; set; }
        public override bool DefaultDownloadLive { get; set; }
        public override bool DefaultKeepShorts { get; set; }
        public override bool DefaultKeepVods { get; set; }
        public override string DefaultRetentionKeepWords { get; set; } = string.Empty;

        protected override AbstractValidator<MetadataSourceSettingsBase> Validator => _validator;
    }
}
