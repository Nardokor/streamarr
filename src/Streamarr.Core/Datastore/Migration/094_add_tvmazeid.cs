using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(94)]
    public class add_tvmazeid : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Series").AddColumn("TvMazeId").AsInt32().WithDefaultValue(0);
            Create.Index().OnTable("Series").OnColumn("TvMazeId");
        }
    }
}
