using FluentValidation;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Core.Configuration;
using Streamarr.Core.Validation;
using Streamarr.Core.Validation.Paths;
using Streamarr.Http;

namespace Streamarr.Api.V1.Settings;

[V1ApiController("settings/mediamanagement")]
public class MediaManagementSettingsController : SettingsController<MediaManagementSettingsResource>
{
    public MediaManagementSettingsController(IConfigService configService,
                                       PathExistsValidator pathExistsValidator,
                                       FolderChmodValidator folderChmodValidator,
                                       FolderWritableValidator folderWritableValidator,
                                       StartupFolderValidator startupFolderValidator,
                                       SystemFolderValidator systemFolderValidator,
                                       RootFolderAncestorValidator rootFolderAncestorValidator,
                                       RootFolderValidator rootFolderValidator)
        : base(configService)
    {
        SharedValidator.RuleFor(c => c.RecycleBinCleanupDays).GreaterThanOrEqualTo(0);
        SharedValidator.RuleFor(c => c.ChmodFolder).SetValidator(folderChmodValidator).When(c => !string.IsNullOrEmpty(c.ChmodFolder) && (OsInfo.IsLinux || OsInfo.IsOsx));

        SharedValidator.RuleFor(c => c.RecycleBin).IsValidPath()
                                                  .SetValidator(folderWritableValidator)
                                                  .SetValidator(rootFolderValidator)
                                                  .SetValidator(pathExistsValidator)
                                                  .SetValidator(rootFolderAncestorValidator)
                                                  .SetValidator(startupFolderValidator)
                                                  .SetValidator(systemFolderValidator)
                                                  .When(c => !string.IsNullOrWhiteSpace(c.RecycleBin));

        SharedValidator.RuleFor(c => c.MinimumFreeSpaceWhenImporting).GreaterThanOrEqualTo(100);
    }

    protected override MediaManagementSettingsResource ToResource(IConfigService model)
    {
        return MediaManagementConfigResourceMapper.ToResource(model);
    }
}
