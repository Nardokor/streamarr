using System;
using System.Collections.Generic;
using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.Validation;

namespace Streamarr.Core.ImportLists.Sonarr
{
    public class SonarrSettingsValidator : AbstractValidator<SonarrSettings>
    {
        public SonarrSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class SonarrSettings : ImportListSettingsBase<SonarrSettings>
    {
        private static readonly SonarrSettingsValidator Validator = new();

        public SonarrSettings()
        {
            ApiKey = "";
            ProfileIds = Array.Empty<int>();
            LanguageProfileIds = Array.Empty<int>();
            TagIds = Array.Empty<int>();
            RootFolderPaths = Array.Empty<string>();
        }

        [FieldDefinition(0, Label = "ImportListsStreamarrSettingsFullUrl", HelpText = "ImportListsStreamarrSettingsFullUrlHelpText")]
        public override string BaseUrl { get; set; } = string.Empty;

        [FieldDefinition(1, Label = "ApiKey", HelpText = "ImportListsStreamarrSettingsApiKeyHelpText")]
        public string ApiKey { get; set; }

        [FieldDefinition(2, Label = "ImportListsStreamarrSettingsSyncSeasonMonitoring", HelpText = "ImportListsStreamarrSettingsSyncSeasonMonitoringHelpText", Type = FieldType.Checkbox)]
        public bool SyncSeasonMonitoring { get; set; }

        [FieldDefinition(3, Type = FieldType.Select, SelectOptionsProviderAction = "getProfiles", Label = "QualityProfiles", HelpText = "ImportListsStreamarrSettingsQualityProfilesHelpText")]
        public IEnumerable<int> ProfileIds { get; set; }

        [FieldDefinition(4, Type = FieldType.Select, SelectOptionsProviderAction = "getTags", Label = "Tags", HelpText = "ImportListsStreamarrSettingsTagsHelpText")]
        public IEnumerable<int> TagIds { get; set; }

        [FieldDefinition(5, Type = FieldType.Select, SelectOptionsProviderAction = "getRootFolders", Label = "RootFolders", HelpText = "ImportListsStreamarrSettingsRootFoldersHelpText")]
        public IEnumerable<string> RootFolderPaths { get; set; }

        // TODO: Remove this eventually, no translation added as deprecated
        [FieldDefinition(6, Type = FieldType.Select, SelectOptionsProviderAction = "getLanguageProfiles", Label = "Language Profiles", HelpText = "Language Profiles from the source instance to import from")]
        public IEnumerable<int> LanguageProfileIds { get; set; }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
