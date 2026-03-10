using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(245)]
    public class channel_sort_order : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Channels")
                 .AddColumn("SortOrder").AsInt32().NotNullable().WithDefaultValue(0);
        }
    }
}
