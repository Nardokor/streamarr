using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(200)]
    public class AddNewItemMonitorType : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Series").AddColumn("MonitorNewItems").AsInt32().WithDefaultValue(0);
            Alter.Table("ImportLists").AddColumn("MonitorNewItems").AsInt32().WithDefaultValue(0);
        }
    }
}
