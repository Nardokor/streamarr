using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(118)]
    public class add_history_eventType_index : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Index().OnTable("History").OnColumn("EventType");
        }
    }
}
