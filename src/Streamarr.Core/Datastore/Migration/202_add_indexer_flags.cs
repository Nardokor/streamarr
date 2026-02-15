using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(202)]
    public class add_indexer_flags : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Blocklist").AddColumn("IndexerFlags").AsInt32().WithDefaultValue(0);
            Alter.Table("EpisodeFiles").AddColumn("IndexerFlags").AsInt32().WithDefaultValue(0);
        }
    }
}
