using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Streamarr.Common.Extensions;
using Streamarr.Core.Annotations;
using Streamarr.Core.Tv;
using Streamarr.Core.Validation;

namespace Streamarr.Core.AutoTagging.Specifications
{
    public class NetworkSpecificationValidator : AbstractValidator<NetworkSpecification>
    {
        public NetworkSpecificationValidator()
        {
            RuleFor(c => c.Value).NotEmpty();
        }
    }

    public class NetworkSpecification : AutoTaggingSpecificationBase
    {
        private static readonly NetworkSpecificationValidator Validator = new();

        public override int Order => 1;
        public override string ImplementationName => "Network";

        [FieldDefinition(1, Label = "AutoTaggingSpecificationNetwork", Type = FieldType.Tag)]
        public IEnumerable<string> Value { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(Series series)
        {
            return Value.Any(network => series.Network.EqualsIgnoreCase(network));
        }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
