using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Streamarr.Common.Extensions;
using Streamarr.Core.Annotations;
using Streamarr.Core.Tv;
using Streamarr.Core.Validation;

namespace Streamarr.Core.AutoTagging.Specifications
{
    public class GenreSpecificationValidator : AbstractValidator<GenreSpecification>
    {
        public GenreSpecificationValidator()
        {
            RuleFor(c => c.Value).NotEmpty();
        }
    }

    public class GenreSpecification : AutoTaggingSpecificationBase
    {
        private static readonly GenreSpecificationValidator Validator = new();

        public override int Order => 1;
        public override string ImplementationName => "Genre";

        [FieldDefinition(1, Label = "AutoTaggingSpecificationGenre", Type = FieldType.Tag)]
        public IEnumerable<string> Value { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(Series series)
        {
            return series.Genres.Any(genre => Value.ContainsIgnoreCase(genre));
        }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
