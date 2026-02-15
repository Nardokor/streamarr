using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(114)]
    public class rename_indexer_status_id : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Rename.Column("IndexerId").OnTable("IndexerStatus").To("ProviderId");
        }
    }
}
