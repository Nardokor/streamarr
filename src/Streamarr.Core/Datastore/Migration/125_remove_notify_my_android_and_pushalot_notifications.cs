using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(125)]
    public class remove_notify_my_android_and_pushalot_notifications : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.FromTable("Notifications").Row(new { Implementation = "NotifyMyAndroid" });
            Delete.FromTable("Notifications").Row(new { Implementation = "Pushalot" });
        }
    }
}
