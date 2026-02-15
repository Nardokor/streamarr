using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(186)]
    public class add_result_to_commands : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Commands").AddColumn("Result").AsInt32().WithDefaultValue(1);
        }
    }
}
