using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.Tv;
using Streamarr.Core.Validation;

namespace Streamarr.Core.AutoTagging.Specifications
{
    public class TagSpecificationValidator : AbstractValidator<TagSpecification>
    {
        public TagSpecificationValidator()
        {
            RuleFor(c => c.Value).GreaterThan(0);
        }
    }

    public class TagSpecification : AutoTaggingSpecificationBase
    {
        private static readonly TagSpecificationValidator Validator = new();

        public override int Order => 1;
        public override string ImplementationName => "Tag";

        [FieldDefinition(1, Label = "AutoTaggingSpecificationTag", Type = FieldType.SeriesTag)]
        public int Value { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(Series series)
        {
            return series.Tags.Contains(Value);
        }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
