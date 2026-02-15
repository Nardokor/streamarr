using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(38)]
    public class add_on_upgrade_to_notifications : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Notifications").AddColumn("OnUpgrade").AsBoolean().Nullable();

            Execute.Sql("UPDATE \"Notifications\" SET \"OnUpgrade\" = \"OnDownload\"");
        }
    }
}
