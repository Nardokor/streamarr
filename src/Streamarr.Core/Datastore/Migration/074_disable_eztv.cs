using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(74)]
    public class disable_eztv : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE \"Indexers\" SET \"EnableRss\" = false, \"EnableSearch\" = false WHERE \"Implementation\" = 'Eztv' AND \"Settings\" LIKE '%ezrss.it%'");
        }
    }
}
