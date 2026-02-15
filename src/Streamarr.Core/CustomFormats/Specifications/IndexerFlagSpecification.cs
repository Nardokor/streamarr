using System;
using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Validation;

namespace Streamarr.Core.CustomFormats
{
    public class IndexerFlagSpecificationValidator : AbstractValidator<IndexerFlagSpecification>
    {
        public IndexerFlagSpecificationValidator()
        {
            RuleFor(c => c.Value).NotEmpty();
            RuleFor(c => c.Value).Custom((flag, context) =>
            {
                if (!Enum.IsDefined(typeof(IndexerFlags), flag))
                {
                    context.AddFailure($"Invalid indexer flag condition value: {flag}");
                }
            });
        }
    }

    public class IndexerFlagSpecification : CustomFormatSpecificationBase
    {
        private static readonly IndexerFlagSpecificationValidator Validator = new();

        public override int Order => 4;
        public override string ImplementationName => "Indexer Flag";

        [FieldDefinition(1, Label = "CustomFormatsSpecificationFlag", Type = FieldType.Select, SelectOptions = typeof(IndexerFlags))]
        public int Value { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            return input.IndexerFlags.HasFlag((IndexerFlags)Value);
        }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
