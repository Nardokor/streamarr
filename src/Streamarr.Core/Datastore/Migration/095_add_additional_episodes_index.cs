using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(95)]
    public class add_additional_episodes_index : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Index().OnTable("Episodes").OnColumn("SeriesId").Ascending()
                                              .OnColumn("SeasonNumber").Ascending()
                                              .OnColumn("EpisodeNumber").Ascending();
        }
    }
}
