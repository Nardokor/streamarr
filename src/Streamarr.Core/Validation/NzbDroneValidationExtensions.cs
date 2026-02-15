using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;

namespace Streamarr.Core.Validation
{
    public static class StreamarrValidationExtensions
    {
        public static StreamarrValidationResult Filter(this StreamarrValidationResult result, params string[] fields)
        {
            var failures = result.Failures.Where(v => fields.Contains(v.PropertyName)).ToArray();

            return new StreamarrValidationResult(failures);
        }

        public static void ThrowOnError(this StreamarrValidationResult result)
        {
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        public static bool HasErrors(this List<ValidationFailure> list)
        {
            return list.Any(item => item is not StreamarrValidationFailure { IsWarning: true });
        }
    }
}
