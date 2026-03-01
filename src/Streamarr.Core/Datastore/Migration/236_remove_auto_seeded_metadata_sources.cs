using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(236)]
    public class Migration236RemoveAutoSeededMetadataSources : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Migration 235 incorrectly auto-inserted a YouTube source on first run.
            // Sources should be added by the user through the Sources settings page.
            Execute.Sql("DELETE FROM MetadataSources WHERE Implementation = 'YouTube'");
        }
    }
}
