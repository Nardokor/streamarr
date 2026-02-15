using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(181)]
    public class quality_definition_preferred_size : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("QualityDefinitions").AddColumn("PreferredSize").AsDouble().Nullable();

            Execute.Sql("UPDATE \"QualityDefinitions\" SET \"PreferredSize\" = \"MaxSize\" - 5 WHERE \"MaxSize\" > 5");
        }
    }
}
