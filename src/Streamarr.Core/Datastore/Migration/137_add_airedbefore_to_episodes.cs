using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(137)]
    public class add_airedbefore_to_episodes : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Episodes").AddColumn("AiredAfterSeasonNumber").AsInt32().Nullable()
                                   .AddColumn("AiredBeforeSeasonNumber").AsInt32().Nullable()
                                   .AddColumn("AiredBeforeEpisodeNumber").AsInt32().Nullable();
        }
    }
}
