using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.Languages;
using Streamarr.Core.Tv;
using Streamarr.Core.Validation;

namespace Streamarr.Core.AutoTagging.Specifications
{
    public class OriginalLanguageSpecificationValidator : AbstractValidator<OriginalLanguageSpecification>
    {
        public OriginalLanguageSpecificationValidator()
        {
            RuleFor(c => c.Value).GreaterThanOrEqualTo(0);
        }
    }

    public class OriginalLanguageSpecification : AutoTaggingSpecificationBase
    {
        private static readonly OriginalLanguageSpecificationValidator Validator = new();

        public override int Order => 1;
        public override string ImplementationName => "Original Language";

        [FieldDefinition(1, Label = "AutoTaggingSpecificationOriginalLanguage", Type = FieldType.Select, SelectOptions = typeof(OriginalLanguageFieldConverter))]
        public int Value { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(Series series)
        {
            return Value == series.OriginalLanguage.Id;
        }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
