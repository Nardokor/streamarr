using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.Validation;
using Streamarr.Core.Validation.Paths;

namespace Streamarr.Core.Download.Clients.Blackhole
{
    public class UsenetBlackholeSettingsValidator : AbstractValidator<UsenetBlackholeSettings>
    {
        public UsenetBlackholeSettingsValidator()
        {
            RuleFor(c => c.NzbFolder).IsValidPath();
            RuleFor(c => c.WatchFolder).IsValidPath();
        }
    }

    public class UsenetBlackholeSettings : DownloadClientSettingsBase<UsenetBlackholeSettings>
    {
        private static readonly UsenetBlackholeSettingsValidator Validator = new();

        [FieldDefinition(0, Label = "UsenetBlackholeNzbFolder", Type = FieldType.Path, HelpText = "BlackholeFolderHelpText")]
        [FieldToken(TokenField.HelpText, "UsenetBlackholeNzbFolder", "extension", ".nzb")]
        public string NzbFolder { get; set; }

        [FieldDefinition(1, Label = "BlackholeWatchFolder", Type = FieldType.Path, HelpText = "BlackholeWatchFolderHelpText")]
        public string WatchFolder { get; set; }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
