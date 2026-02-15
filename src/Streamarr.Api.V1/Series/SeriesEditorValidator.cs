using FluentValidation;
using Streamarr.Common.Extensions;
using Streamarr.Core.Validation;
using Streamarr.Core.Validation.Paths;

namespace Streamarr.Api.V1.Series;

public class SeriesEditorValidator : AbstractValidator<Streamarr.Core.Tv.Series>
{
    public SeriesEditorValidator(RootFolderExistsValidator rootFolderExistsValidator, QualityProfileExistsValidator qualityProfileExistsValidator)
    {
        RuleFor(s => s.RootFolderPath).Cascade(CascadeMode.Stop)
            .IsValidPath()
            .SetValidator(rootFolderExistsValidator)
            .When(s => s.RootFolderPath.IsNotNullOrWhiteSpace());

        RuleFor(c => c.QualityProfileId).Cascade(CascadeMode.Stop)
            .ValidId()
            .SetValidator(qualityProfileExistsValidator);
    }
}
