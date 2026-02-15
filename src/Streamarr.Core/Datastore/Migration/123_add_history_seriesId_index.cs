using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(123)]
    public class add_history_seriesId_index : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Index().OnTable("History").OnColumn("SeriesId");
        }
    }
}
