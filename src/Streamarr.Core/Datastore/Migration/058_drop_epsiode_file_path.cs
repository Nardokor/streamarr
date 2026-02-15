using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(58)]
    public class drop_episode_file_path : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("Path").FromTable("EpisodeFiles");
        }
    }
}
