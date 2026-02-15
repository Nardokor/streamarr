using Streamarr.Core.Validation;

namespace Streamarr.Core.CustomFormats
{
    public interface ICustomFormatSpecification
    {
        int Order { get; }
        string InfoLink { get; }
        string ImplementationName { get; }
        string Name { get; set; }
        bool Negate { get; set; }
        bool Required { get; set; }

        StreamarrValidationResult Validate();

        ICustomFormatSpecification Clone();
        bool IsSatisfiedBy(CustomFormatInput input);
    }
}
