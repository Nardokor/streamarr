using Streamarr.Core.Validation;

namespace Streamarr.Core.ThingiProvider
{
    public class NullConfig : IProviderConfig
    {
        public static readonly NullConfig Instance = new NullConfig();

        public StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult();
        }
    }
}
