using Streamarr.Core.Configuration;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Settings;

public class MediaManagementSettingsResource : RestResource
{
    public string? RecycleBin { get; set; }
    public int RecycleBinCleanupDays { get; set; }
    public bool DeleteEmptyFolders { get; set; }

    public bool SetPermissionsLinux { get; set; }
    public string? ChmodFolder { get; set; }
    public string? ChownGroup { get; set; }

    public bool SkipFreeSpaceCheckWhenImporting { get; set; }
    public int MinimumFreeSpaceWhenImporting { get; set; }
    public bool CopyUsingHardlinks { get; set; }
}

public static class MediaManagementConfigResourceMapper
{
    public static MediaManagementSettingsResource ToResource(IConfigService model)
    {
        return new MediaManagementSettingsResource
        {
            RecycleBin = model.RecycleBin,
            RecycleBinCleanupDays = model.RecycleBinCleanupDays,
            DeleteEmptyFolders = model.DeleteEmptyFolders,

            SetPermissionsLinux = model.SetPermissionsLinux,
            ChmodFolder = model.ChmodFolder,
            ChownGroup = model.ChownGroup,

            SkipFreeSpaceCheckWhenImporting = model.SkipFreeSpaceCheckWhenImporting,
            MinimumFreeSpaceWhenImporting = model.MinimumFreeSpaceWhenImporting,
            CopyUsingHardlinks = model.CopyUsingHardlinks
        };
    }
}
