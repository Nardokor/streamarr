using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.Tv;
using Streamarr.Core.Validation;

namespace Streamarr.Core.AutoTagging.Specifications
{
    public class QualityProfileSpecificationValidator : AbstractValidator<QualityProfileSpecification>
    {
        public QualityProfileSpecificationValidator()
        {
            RuleFor(c => c.Value).GreaterThan(0);
        }
    }

    public class QualityProfileSpecification : AutoTaggingSpecificationBase
    {
        private static readonly QualityProfileSpecificationValidator Validator = new();

        public override int Order => 1;
        public override string ImplementationName => "Quality Profile";

        [FieldDefinition(1, Label = "AutoTaggingSpecificationQualityProfile", Type = FieldType.QualityProfile)]
        public int Value { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(Series series)
        {
            return Value == series.QualityProfileId;
        }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
