using System.Linq;
using FluentValidation.Validators;
using Streamarr.Common.Disk;
using Streamarr.Common.Extensions;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Validation.Paths
{
    public class SeriesPathValidator : PropertyValidator
    {
        private readonly ISeriesService _seriesService;

        public SeriesPathValidator(ISeriesService seriesService)
        {
            _seriesService = seriesService;
        }

        protected override string GetDefaultMessageTemplate() => "Path '{path}' is already configured for another series";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            context.MessageFormatter.AppendArgument("path", context.PropertyValue.ToString());

            dynamic instance = context.ParentContext.InstanceToValidate;
            var instanceId = (int)instance.Id;

            // Skip the path for this series and any invalid paths
            return !_seriesService.GetAllSeriesPaths().Any(s => s.Key != instanceId &&
                                                                s.Value.IsPathValid(PathValidationType.CurrentOs) &&
                                                                s.Value.PathEquals(context.PropertyValue.ToString()));
        }
    }
}
