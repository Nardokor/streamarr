using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(16)]
    public class updated_imported_history_item : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE \"History\" SET \"Data\" = replace( \"Data\", '\"Path\"', '\"ImportedPath\"' ) WHERE \"EventType\" = 3");
        }
    }
}
