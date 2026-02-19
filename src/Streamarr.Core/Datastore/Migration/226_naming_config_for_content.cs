using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(226)]
    public class naming_config_for_content : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Drop the TV-specific NamingConfig table entirely and recreate for streaming content.
            // The old table schema varied depending on how far Sonarr had migrated, so
            // a clean drop+recreate is safer than trying to delete individual columns.
            Delete.Table("NamingConfig");

            Create.TableForModel("NamingConfig")
                  .WithColumn("RenameContent").AsBoolean().WithDefaultValue(true)
                  .WithColumn("ReplaceIllegalCharacters").AsBoolean().WithDefaultValue(true)
                  .WithColumn("ColonReplacementFormat").AsInt32().WithDefaultValue(4)
                  .WithColumn("ContentFileFormat").AsString().WithDefaultValue("{Published Date} - {Content Title} [{Content Id}]")
                  .WithColumn("CreatorFolderFormat").AsString().WithDefaultValue("{Creator Title}");
        }
    }
}
