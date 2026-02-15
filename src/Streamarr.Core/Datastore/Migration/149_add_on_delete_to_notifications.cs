using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(149)]
    public class add_on_delete_to_notifications : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Notifications").AddColumn("OnSeriesDelete").AsBoolean().WithDefaultValue(false);
            Alter.Table("Notifications").AddColumn("OnEpisodeFileDelete").AsBoolean().WithDefaultValue(false);
        }
    }
}
