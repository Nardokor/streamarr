using FluentValidation;
using Streamarr.Core.ThingiProvider;
using Streamarr.Core.Validation;

namespace Streamarr.Core.Notifications
{
    public abstract class NotificationSettingsBase<TSettings> : IProviderConfig
        where TSettings : NotificationSettingsBase<TSettings>
    {
        protected abstract AbstractValidator<TSettings> Validator { get; }

        public StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate((TSettings)this));
        }
    }
}
