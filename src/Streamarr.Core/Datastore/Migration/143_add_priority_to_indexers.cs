using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(143)]
    public class add_priority_to_indexers : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Indexers").AddColumn("Priority").AsInt32().NotNullable().WithDefaultValue(25);
        }
    }
}
