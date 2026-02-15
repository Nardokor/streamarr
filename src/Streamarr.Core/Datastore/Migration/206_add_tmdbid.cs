using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(206)]
    public class add_tmdbid : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Series").AddColumn("TmdbId").AsInt32().WithDefaultValue(0);
            Create.Index().OnTable("Series").OnColumn("TmdbId");
        }
    }
}
