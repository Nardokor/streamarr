using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.Tv;
using Streamarr.Core.Validation;

namespace Streamarr.Core.AutoTagging.Specifications
{
    public class StatusSpecificationValidator : AbstractValidator<StatusSpecification>
    {
    }

    public class StatusSpecification : AutoTaggingSpecificationBase
    {
        private static readonly StatusSpecificationValidator Validator = new();

        public override int Order => 1;
        public override string ImplementationName => "Status";

        [FieldDefinition(1, Label = "AutoTaggingSpecificationStatus", Type = FieldType.Select, SelectOptions = typeof(SeriesStatusType))]
        public int Status { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(Series series)
        {
            return series.Status == (SeriesStatusType)Status;
        }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
