using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(7)]
    public class add_renameEpisodes_to_naming : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("NamingConfig")
                .AddColumn("RenameEpisodes")
                .AsBoolean()
                .Nullable();

            Execute.Sql("UPDATE \"NamingConfig\" SET \"RenameEpisodes\" = NOT \"UseSceneName\"");
        }
    }
}
