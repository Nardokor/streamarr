using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(5)]
    public class added_eventtype_to_history : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("History")
                .AddColumn("EventType")
                .AsInt32()
                .Nullable();
        }
    }
}
