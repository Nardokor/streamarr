using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(165)]
    public class add_on_update_to_notifications : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Notifications").AddColumn("OnApplicationUpdate").AsBoolean().WithDefaultValue(false);
        }
    }
}
