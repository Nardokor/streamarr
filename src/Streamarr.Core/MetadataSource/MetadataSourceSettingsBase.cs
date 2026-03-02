using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.ThingiProvider;
using Streamarr.Core.Validation;

namespace Streamarr.Core.MetadataSource
{
    public abstract class MetadataSourceSettingsBase : IProviderConfig
    {
        protected abstract AbstractValidator<MetadataSourceSettingsBase> Validator { get; }

        public StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }

        // Sync scheduling
        [FieldDefinition(90, Label = "Refresh Interval", Unit = "hours", HelpText = "How often to sync new content from this source (hours).")]
        public int RefreshIntervalHours { get; set; } = 24;

        [FieldDefinition(91, Label = "Live Check Interval", Unit = "minutes", HelpText = "How often to check the status of active livestreams (minutes).")]
        public int LiveCheckIntervalMinutes { get; set; } = 60;

        // Default channel filter settings applied when a new channel is added from this source
        [FieldDefinition(100, Label = "Download Videos", Type = FieldType.Checkbox, HelpText = "Download regular video uploads by default for new channels.")]
        public bool DefaultDownloadVideos { get; set; }

        [FieldDefinition(101, Label = "Download Shorts", Type = FieldType.Checkbox, HelpText = "Download short-form content by default for new channels.")]
        public bool DefaultDownloadShorts { get; set; }

        [FieldDefinition(102, Label = "Download VODs", Type = FieldType.Checkbox, HelpText = "Download archived livestreams by default for new channels.")]
        public bool DefaultDownloadVods { get; set; }

        [FieldDefinition(103, Label = "Download Live", Type = FieldType.Checkbox, HelpText = "Record active livestreams by default for new channels.")]
        public bool DefaultDownloadLive { get; set; } = true;

        [FieldDefinition(104, Label = "Watched Words", HelpText = "Comma-separated words that must appear in the title. Empty = watch everything.")]
        public string DefaultWatchedWords { get; set; } = string.Empty;

        [FieldDefinition(105, Label = "Ignored Words", HelpText = "Comma-separated words that exclude a title from downloading.")]
        public string DefaultIgnoredWords { get; set; } = string.Empty;

        [FieldDefinition(106, Label = "Watched Defeats Ignored", Type = FieldType.Checkbox, HelpText = "When a title matches both watched and ignored words, download it.")]
        public bool DefaultWatchedDefeatsIgnored { get; set; } = true;

        [FieldDefinition(107, Label = "Auto Download", Type = FieldType.Checkbox, HelpText = "Automatically queue matched content for download.")]
        public bool DefaultAutoDownload { get; set; }
    }
}
