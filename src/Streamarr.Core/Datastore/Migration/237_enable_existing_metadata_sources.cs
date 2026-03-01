using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(237)]
    public class Migration237EnableExistingMetadataSources : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Sources saved before the Enable toggle was added to the UI defaulted to
            // Enable = false. Flip them on so they are immediately usable.
            Execute.Sql("UPDATE MetadataSources SET Enable = 1 WHERE Enable = 0");
        }
    }
}
