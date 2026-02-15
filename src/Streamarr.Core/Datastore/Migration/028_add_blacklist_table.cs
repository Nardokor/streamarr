using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(28)]
    public class add_blacklist_table : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("Blacklist")
                .WithColumn("SeriesId").AsInt32()
                .WithColumn("EpisodeIds").AsString()
                .WithColumn("SourceTitle").AsString()
                .WithColumn("Quality").AsString()
                .WithColumn("Date").AsDateTime();
        }
    }
}
