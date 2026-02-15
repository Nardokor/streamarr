using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(116)]
    public class disable_nyaa : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE \"Indexers\" SET \"EnableRss\" = false, \"EnableSearch\" = false, \"Settings\" = Replace(\"Settings\", 'https://nyaa.se', '') WHERE \"Implementation\" = 'Nyaa' AND \"Settings\" LIKE '%nyaa.se%';");
        }
    }
}
