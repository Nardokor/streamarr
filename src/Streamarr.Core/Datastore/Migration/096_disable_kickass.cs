using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(96)]
    public class disable_kickass : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE \"Indexers\" SET \"EnableRss\" = false, \"EnableSearch\" = false, \"Settings\" = Replace(\"Settings\", 'https://kat.cr', '') WHERE \"Implementation\" = 'KickassTorrents' AND \"Settings\" LIKE '%kat.cr%';");
        }
    }
}
