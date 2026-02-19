using Streamarr.Core.Datastore;

namespace Streamarr.Core.Organizer
{
    public class NamingConfig : ModelBase
    {
        public static NamingConfig Default => new NamingConfig
        {
            RenameContent = true,
            ReplaceIllegalCharacters = true,
            ColonReplacementFormat = ColonReplacementFormat.Smart,
            ContentFileFormat = "{Published Date} - {Content Title} [{Content Id}]",
            CreatorFolderFormat = "{Creator Title}"
        };

        public bool RenameContent { get; set; }
        public bool ReplaceIllegalCharacters { get; set; }
        public ColonReplacementFormat ColonReplacementFormat { get; set; }
        public string ContentFileFormat { get; set; } = string.Empty;
        public string CreatorFolderFormat { get; set; } = string.Empty;
    }

    public enum ColonReplacementFormat
    {
        Delete = 0,
        Dash = 1,
        SpaceDash = 2,
        SpaceDashSpace = 3,
        Smart = 4
    }
}
