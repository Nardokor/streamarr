using FluentValidation;
using Streamarr.Common.Extensions;
using Streamarr.Core.Annotations;
using Streamarr.Core.Tv;
using Streamarr.Core.Validation;
using Streamarr.Core.Validation.Paths;

namespace Streamarr.Core.AutoTagging.Specifications
{
    public class RootFolderSpecificationValidator : AbstractValidator<RootFolderSpecification>
    {
        public RootFolderSpecificationValidator()
        {
            RuleFor(c => c.Value).IsValidPath();
        }
    }

    public class RootFolderSpecification : AutoTaggingSpecificationBase
    {
        private static readonly RootFolderSpecificationValidator Validator = new RootFolderSpecificationValidator();

        public override int Order => 1;
        public override string ImplementationName => "Root Folder";

        [FieldDefinition(1, Label = "AutoTaggingSpecificationRootFolder", Type = FieldType.RootFolder)]
        public string Value { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(Series series)
        {
            return series.RootFolderPath.PathEquals(Value);
        }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
