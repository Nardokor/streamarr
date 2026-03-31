using FluentValidation;

namespace Streamarr.Core.MetadataSource.Patreon
{
    public class PatreonSettingsValidator : AbstractValidator<MetadataSourceSettingsBase>
    {
        public PatreonSettingsValidator()
        {
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
