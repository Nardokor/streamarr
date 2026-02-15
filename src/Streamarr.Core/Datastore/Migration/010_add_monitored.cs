using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(10)]
    public class add_monitored : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Episodes").AddColumn("Monitored").AsBoolean().Nullable();
            Alter.Table("Seasons").AddColumn("Monitored").AsBoolean().Nullable();

            Update.Table("Episodes").Set(new { Monitored = true }).Where(new { Ignored = false });
            Update.Table("Episodes").Set(new { Monitored = false }).Where(new { Ignored = true });

            Update.Table("Seasons").Set(new { Monitored = true }).Where(new { Ignored = false });
            Update.Table("Seasons").Set(new { Monitored = false }).Where(new { Ignored = true });
        }
    }
}
