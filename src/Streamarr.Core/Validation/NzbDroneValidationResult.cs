using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Streamarr.Common.Extensions;

namespace Streamarr.Core.Validation
{
    public class StreamarrValidationResult : ValidationResult
    {
        public StreamarrValidationResult()
        {
            Failures = new List<StreamarrValidationFailure>();
            Errors = new List<StreamarrValidationFailure>();
            Warnings = new List<StreamarrValidationFailure>();
        }

        public StreamarrValidationResult(ValidationResult validationResult)
            : this(validationResult.Errors)
        {
        }

        public StreamarrValidationResult(IEnumerable<ValidationFailure> failures)
        {
            var errors = new List<StreamarrValidationFailure>();
            var warnings = new List<StreamarrValidationFailure>();

            foreach (var failureBase in failures)
            {
                if (failureBase is not StreamarrValidationFailure failure)
                {
                    failure = new StreamarrValidationFailure(failureBase);
                }

                if (failure.IsWarning)
                {
                    warnings.Add(failure);
                }
                else
                {
                    errors.Add(failure);
                }
            }

            Failures = errors.Concat(warnings).ToList();
            Errors = errors;
            errors.ForEach(base.Errors.Add);
            Warnings = warnings;
        }

        public IList<StreamarrValidationFailure> Failures { get; private set; }
        public new IList<StreamarrValidationFailure> Errors { get; private set; }
        public IList<StreamarrValidationFailure> Warnings { get; private set; }

        public virtual bool HasWarnings => Warnings.Any();

        public override bool IsValid => Errors.Empty();
    }
}
