using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(209)]
    public class add_on_import_complete_to_notifications : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Notifications").AddColumn("OnImportComplete").AsBoolean().WithDefaultValue(false);
        }
    }
}
