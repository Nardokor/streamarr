using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.Channels;
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

        // Managed by the application — set automatically when a cookie file is uploaded via the API.
        // Not decorated with [FieldDefinition] so it is excluded from the settings form.
        public string CookiesFilePath { get; set; } = string.Empty;

        // Sync scheduling — this interval also governs live stream detection.
        // Default 1 hour keeps live streams discoverable without burning API quota.
        [FieldDefinition(90, Label = "Refresh Interval", Unit = "hours", HelpText = "How often to sync new content and detect live streams (hours). Values above 6 may cause recent uploads and live streams to be missed.")]
        public int RefreshIntervalHours { get; set; } = 1;

        // Default channel filter settings applied when a new channel is added from this source
        [FieldDefinition(100, Label = "Download Videos", Type = FieldType.Checkbox, HelpText = "Download regular videos by default for new channels.")]
        public virtual bool DefaultDownloadVideos { get; set; } = true;

        [FieldDefinition(101, Label = "Download Shorts", Type = FieldType.Checkbox, HelpText = "Download shorts by default for new channels.")]
        public virtual bool DefaultDownloadShorts { get; set; } = true;

        [FieldDefinition(102, Label = "Download VODs", Type = FieldType.Checkbox, HelpText = "Download archived livestreams by default for new channels.")]
        public virtual bool DefaultDownloadVods { get; set; }

        [FieldDefinition(103, Label = "Download Live", Type = FieldType.Checkbox, HelpText = "Record active livestreams by default for new channels.")]
        public virtual bool DefaultDownloadLive { get; set; } = true;

        [FieldDefinition(104, Label = "Watched Words", HelpText = "Comma-separated words that must appear in the title. Empty = watch everything.")]
        public virtual string DefaultWatchedWords { get; set; } = string.Empty;

        [FieldDefinition(105, Label = "Ignored Words", HelpText = "Comma-separated words that exclude a title from downloading.")]
        public virtual string DefaultIgnoredWords { get; set; } = string.Empty;

        [FieldDefinition(106, Label = "Watched Defeats Ignored", Type = FieldType.Checkbox, HelpText = "When a title matches both watched and ignored words, download it.")]
        public virtual bool DefaultWatchedDefeatsIgnored { get; set; } = true;

        [FieldDefinition(107, Label = "Auto Download", Type = FieldType.Checkbox, HelpText = "Automatically queue matched content for download.")]
        public virtual bool DefaultAutoDownload { get; set; }

        // Default retention settings — hidden until the retention UI is fully implemented.
        [FieldDefinition(110, Label = "Retention Days", Type = FieldType.Number, Unit = "days", HelpText = "Delete downloaded content older than this many days by default for new channels. 0 = disabled.", Hidden = HiddenType.Hidden)]
        public int DefaultRetentionDays { get; set; }

        [FieldDefinition(111, Label = "Always Keep: Videos", Type = FieldType.Checkbox, HelpText = "Never delete regular videos by default for new channels.", Hidden = HiddenType.Hidden)]
        public virtual bool DefaultKeepVideos { get; set; } = true;

        [FieldDefinition(112, Label = "Always Keep: Shorts", Type = FieldType.Checkbox, HelpText = "Never delete shorts by default for new channels.", Hidden = HiddenType.Hidden)]
        public virtual bool DefaultKeepShorts { get; set; } = true;

        [FieldDefinition(113, Label = "Always Keep: VODs", Type = FieldType.Checkbox, HelpText = "Never delete VODs by default for new channels.", Hidden = HiddenType.Hidden)]
        public virtual bool DefaultKeepVods { get; set; }

        [FieldDefinition(114, Label = "Keep Words", HelpText = "Comma-separated words — matching titles are never deleted. Applied by default for new channels.", Hidden = HiddenType.Hidden)]
        public virtual string DefaultRetentionKeepWords { get; set; } = string.Empty;

        public void ApplyDefaultsTo(Channel channel)
        {
            channel.DownloadVideos = DefaultDownloadVideos;
            channel.DownloadShorts = DefaultDownloadShorts;
            channel.DownloadVods = DefaultDownloadVods;
            channel.DownloadLive = DefaultDownloadLive;
            channel.WatchedWords = DefaultWatchedWords;
            channel.IgnoredWords = DefaultIgnoredWords;
            channel.WatchedDefeatsIgnored = DefaultWatchedDefeatsIgnored;
            channel.AutoDownload = DefaultAutoDownload;
            channel.RetentionDays = DefaultRetentionDays == 0 ? null : (int?)DefaultRetentionDays;
            channel.KeepVideos = DefaultKeepVideos;
            channel.KeepShorts = DefaultKeepShorts;
            channel.KeepVods = DefaultKeepVods;
            channel.RetentionKeepWords = DefaultRetentionKeepWords;
        }
    }
}
