using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(24)]
    public class drop_tvdb_episodeid : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("TvDbEpisodeId").FromTable("Episodes");
        }
    }
}
