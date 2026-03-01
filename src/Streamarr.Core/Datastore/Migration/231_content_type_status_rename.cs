using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(231)]
    public class content_type_status_rename : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // ContentStatus 5 was "Live" (stream currently airing), now renamed to "Recording"
            // (actively capturing). Any rows left in this state from a previous run were not
            // actually being recorded, so reset them to Missing (1) so they are re-evaluated.
            Execute.Sql("UPDATE \"Contents\" SET \"Status\" = 1 WHERE \"Status\" = 5");
        }
    }
}
