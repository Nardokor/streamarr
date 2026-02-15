using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(21)]
    public class drop_seasons_table : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Table("Seasons");
        }
    }
}
