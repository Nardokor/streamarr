using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(197)]
    public class list_add_missing_search : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("ImportLists").AddColumn("SearchForMissingEpisodes").AsBoolean().NotNullable().WithDefaultValue(true);
        }
    }
}
