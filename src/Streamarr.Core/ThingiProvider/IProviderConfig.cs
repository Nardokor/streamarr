using Streamarr.Core.Validation;

namespace Streamarr.Core.ThingiProvider
{
    public interface IProviderConfig
    {
        StreamarrValidationResult Validate();
    }
}
