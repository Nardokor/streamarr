using FluentValidation.Results;

namespace Streamarr.Core.Validation
{
    public class StreamarrValidationFailure : ValidationFailure
    {
        public bool IsWarning { get; set; }
        public string DetailedDescription { get; set; }
        public string InfoLink { get; set; }

        public StreamarrValidationFailure(string propertyName, string error)
            : base(propertyName, error)
        {
        }

        public StreamarrValidationFailure(string propertyName, string error, object attemptedValue)
            : base(propertyName, error, attemptedValue)
        {
        }

        public StreamarrValidationFailure(ValidationFailure validationFailure)
            : base(validationFailure.PropertyName, validationFailure.ErrorMessage, validationFailure.AttemptedValue)
        {
            CustomState = validationFailure.CustomState;
            var state = validationFailure.CustomState as StreamarrValidationState;

            IsWarning = state is { IsWarning: true };
        }
    }
}
