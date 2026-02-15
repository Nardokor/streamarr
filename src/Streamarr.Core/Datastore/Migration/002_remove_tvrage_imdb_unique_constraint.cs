using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(2)]
    public class remove_tvrage_imdb_unique_constraint : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Index().OnTable("Series").OnColumn("TvRageId");
            Delete.Index().OnTable("Series").OnColumn("ImdbId");
        }
    }
}
