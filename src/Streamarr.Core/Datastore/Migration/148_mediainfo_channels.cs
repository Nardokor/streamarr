using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(148)]
    public class mediainfo_channels : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE \"EpisodeFiles\" SET \"MediaInfo\" = Replace(\"MediaInfo\", '\"audioChannels\"', '\"audioChannelsContainer\"');");
            Execute.Sql("UPDATE \"EpisodeFiles\" SET \"MediaInfo\" = Replace(\"MediaInfo\", '\"audioChannelPositionsText\"', '\"audioChannelPositionsTextContainer\"');");
        }
    }
}
