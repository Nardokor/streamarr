using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(129)]
    public class add_relative_original_path_to_episode_file : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("EpisodeFiles").AddColumn("OriginalFilePath").AsString().Nullable();
        }
    }
}
