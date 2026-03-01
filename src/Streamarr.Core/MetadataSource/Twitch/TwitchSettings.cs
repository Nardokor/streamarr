using FluentValidation;
using Streamarr.Core.Annotations;

namespace Streamarr.Core.MetadataSource.Twitch
{
    public class TwitchSettingsValidator : AbstractValidator<MetadataSourceSettingsBase>
    {
    }

    public class TwitchSettings : MetadataSourceSettingsBase
    {
        private static readonly TwitchSettingsValidator _validator = new();

        [FieldDefinition(0, Label = "Client ID", HelpText = "Twitch application Client ID from dev.twitch.tv/console.")]
        public string ClientId { get; set; } = string.Empty;

        [FieldDefinition(1, Label = "Client Secret", Type = FieldType.Password, Privacy = PrivacyLevel.Password, HelpText = "Twitch application Client Secret from dev.twitch.tv/console.")]
        public string ClientSecret { get; set; } = string.Empty;

        protected override AbstractValidator<MetadataSourceSettingsBase> Validator => _validator;
    }
}
