using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(59)]
    public class add_enable_options_to_indexers : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Indexers")
                 .AddColumn("EnableRss").AsBoolean().Nullable()
                 .AddColumn("EnableSearch").AsBoolean().Nullable();

            Execute.Sql("UPDATE \"Indexers\" SET \"EnableRss\" = \"Enable\", \"EnableSearch\" = \"Enable\"");
            Update.Table("Indexers").Set(new { EnableSearch = false }).Where(new { Implementation = "Wombles" });
        }
    }
}
