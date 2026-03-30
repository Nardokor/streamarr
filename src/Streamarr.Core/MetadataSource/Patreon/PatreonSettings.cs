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

        // Patreon posts are the primary content type — use "Posts" label instead of "Videos".
        [FieldDefinition(100, Label = "Download Posts", Type = FieldType.Checkbox, HelpText = "Download posts by default for new channels.")]
        public override bool DefaultDownloadVideos { get; set; } = true;

        // Patreon has no shorts, VODs, or live streams.
        [FieldDefinition(101, Hidden = HiddenType.Hidden)]
        public override bool DefaultDownloadShorts { get; set; }

        [FieldDefinition(102, Hidden = HiddenType.Hidden)]
        public override bool DefaultDownloadVods { get; set; }

        [FieldDefinition(103, Hidden = HiddenType.Hidden)]
        public override bool DefaultDownloadLive { get; set; }

        protected override AbstractValidator<MetadataSourceSettingsBase> Validator => _validator;
    }
}
