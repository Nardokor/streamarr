using Streamarr.Core.Tv;
using Streamarr.Core.Validation;

namespace Streamarr.Core.AutoTagging.Specifications
{
    public interface IAutoTaggingSpecification
    {
        int Order { get; }
        string ImplementationName { get; }
        string Name { get; set; }
        bool Negate { get; set; }
        bool Required { get; set; }
        StreamarrValidationResult Validate();

        IAutoTaggingSpecification Clone();
        bool IsSatisfiedBy(Series series);
    }
}
