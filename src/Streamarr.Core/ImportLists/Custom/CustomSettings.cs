using FluentValidation;

using Streamarr.Core.Annotations;
using Streamarr.Core.Validation;

namespace Streamarr.Core.ImportLists.Custom
{
    public class CustomSettingsValidator : AbstractValidator<CustomSettings>
    {
        public CustomSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
        }
    }

    public class CustomSettings : ImportListSettingsBase<CustomSettings>
    {
        private static readonly CustomSettingsValidator Validator = new();

        [FieldDefinition(0, Label = "ImportListsCustomListSettingsUrl", HelpText = "ImportListsCustomListSettingsUrlHelpText")]
        public override string BaseUrl { get; set; } = string.Empty;

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
