using Streamarr.Core.Organizer;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Settings;

public class NamingSettingsResource : RestResource
{
    public bool RenameContent { get; set; }
    public bool ReplaceIllegalCharacters { get; set; }
    public int ColonReplacementFormat { get; set; }
    public string ContentFileFormat { get; set; } = string.Empty;
    public string CreatorFolderFormat { get; set; } = string.Empty;
}

public static class NamingSettingsResourceMapper
{
    public static NamingSettingsResource ToResource(this NamingConfig config)
    {
        return new NamingSettingsResource
        {
            Id = config.Id,
            RenameContent = config.RenameContent,
            ReplaceIllegalCharacters = config.ReplaceIllegalCharacters,
            ColonReplacementFormat = (int)config.ColonReplacementFormat,
            ContentFileFormat = config.ContentFileFormat,
            CreatorFolderFormat = config.CreatorFolderFormat
        };
    }

    public static NamingConfig ToModel(this NamingSettingsResource resource)
    {
        return new NamingConfig
        {
            Id = resource.Id,
            RenameContent = resource.RenameContent,
            ReplaceIllegalCharacters = resource.ReplaceIllegalCharacters,
            ColonReplacementFormat = (ColonReplacementFormat)resource.ColonReplacementFormat,
            ContentFileFormat = resource.ContentFileFormat,
            CreatorFolderFormat = resource.CreatorFolderFormat
        };
    }
}
