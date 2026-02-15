using FluentValidation.Results;
using Streamarr.Common.Extensions;

namespace Streamarr.Api.V1.Provider
{
    public class ProviderTestAllResult
    {
        public int Id { get; set; }
        public bool IsValid => ValidationFailures.Empty();
        public List<ValidationFailure> ValidationFailures { get; set; }

        public ProviderTestAllResult()
        {
            ValidationFailures = new List<ValidationFailure>();
        }
    }
}
