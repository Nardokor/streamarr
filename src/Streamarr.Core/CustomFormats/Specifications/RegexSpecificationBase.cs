using System.Text.RegularExpressions;
using FluentValidation;
using Streamarr.Common.Extensions;
using Streamarr.Core.Annotations;
using Streamarr.Core.Validation;

namespace Streamarr.Core.CustomFormats
{
    public class RegexSpecificationBaseValidator : AbstractValidator<RegexSpecificationBase>
    {
        public RegexSpecificationBaseValidator()
        {
            RuleFor(c => c.Value).NotEmpty().WithMessage("Regex Pattern must not be empty");
        }
    }

    public abstract class RegexSpecificationBase : CustomFormatSpecificationBase
    {
        private static readonly RegexSpecificationBaseValidator Validator = new RegexSpecificationBaseValidator();

        protected Regex _regex;
        protected string _raw;

        [FieldDefinition(1, Label = "CustomFormatsSpecificationRegularExpression", HelpText = "CustomFormatsSpecificationRegularExpressionHelpText")]
        public string Value
        {
            get => _raw;
            set
            {
                _raw = value;

                if (value.IsNotNullOrWhiteSpace())
                {
                    _regex = new Regex(value, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
            }
        }

        protected bool MatchString(string compared)
        {
            if (compared == null || _regex == null)
            {
                return false;
            }

            return _regex.IsMatch(compared);
        }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
