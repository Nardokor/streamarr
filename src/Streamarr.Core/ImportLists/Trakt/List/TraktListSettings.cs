using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.Validation;

namespace Streamarr.Core.ImportLists.Trakt.List
{
    public class TraktListSettingsValidator : TraktSettingsBaseValidator<TraktListSettings>
    {
        public TraktListSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Listname).NotEmpty();
        }
    }

    public class TraktListSettings : TraktSettingsBase<TraktListSettings>
    {
        private static readonly TraktListSettingsValidator Validator = new();

        [FieldDefinition(1, Label = "Username", HelpText = "ImportListsTraktSettingsUsernameHelpText")]
        public string Username { get; set; }

        [FieldDefinition(2, Label = "ImportListsTraktSettingsListName", HelpText = "ImportListsTraktSettingsListNameHelpText")]
        public string Listname { get; set; }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
