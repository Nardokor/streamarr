using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(35)]
    public class add_series_folder_format_to_naming_config : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("NamingConfig").AddColumn("SeriesFolderFormat").AsString().Nullable();

            Update.Table("NamingConfig").Set(new { SeriesFolderFormat = "{Series Title}" }).AllRows();
        }
    }
}
