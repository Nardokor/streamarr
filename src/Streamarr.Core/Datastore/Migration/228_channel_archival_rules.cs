using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(228)]
    public class channel_archival_rules : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Channels")
                 .AddColumn("PriorityFilter").AsString().NotNullable().WithDefaultValue(string.Empty)
                 .AddColumn("RetentionDays").AsInt32().Nullable();
        }
    }
}
