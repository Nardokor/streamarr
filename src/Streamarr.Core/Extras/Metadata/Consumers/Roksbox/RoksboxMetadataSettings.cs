using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.ThingiProvider;
using Streamarr.Core.Validation;

namespace Streamarr.Core.Extras.Metadata.Consumers.Roksbox
{
    public class RoksboxSettingsValidator : AbstractValidator<RoksboxMetadataSettings>
    {
    }

    public class RoksboxMetadataSettings : IProviderConfig
    {
        private static readonly RoksboxSettingsValidator Validator = new RoksboxSettingsValidator();

        public RoksboxMetadataSettings()
        {
            EpisodeMetadata = true;
            SeriesImages = true;
            SeasonImages = true;
            EpisodeImages = true;
        }

        [FieldDefinition(0, Label = "MetadataSettingsEpisodeMetadata", Type = FieldType.Checkbox, Section = MetadataSectionType.Metadata, HelpText = "Season##\\filename.xml")]
        public bool EpisodeMetadata { get; set; }

        [FieldDefinition(1, Label = "MetadataSettingsSeriesImages", Type = FieldType.Checkbox, Section = MetadataSectionType.Image, HelpText = "Series Title.jpg")]
        public bool SeriesImages { get; set; }

        [FieldDefinition(2, Label = "MetadataSettingsSeasonImages", Type = FieldType.Checkbox, Section = MetadataSectionType.Image, HelpText = "Season ##.jpg")]
        public bool SeasonImages { get; set; }

        [FieldDefinition(3, Label = "MetadataSettingsEpisodeImages", Type = FieldType.Checkbox, Section = MetadataSectionType.Image, HelpText = "Season##\\filename.jpg")]
        public bool EpisodeImages { get; set; }

        public bool IsValid => true;

        public StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
