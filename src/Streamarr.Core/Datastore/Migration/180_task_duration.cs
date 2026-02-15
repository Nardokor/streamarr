using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(180)]
    public class task_duration : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("ScheduledTasks").AddColumn("LastStartTime").AsDateTime().Nullable();
        }
    }
}
