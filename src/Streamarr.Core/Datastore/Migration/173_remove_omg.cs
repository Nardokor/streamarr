using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(173)]
    public class remove_omg : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("DELETE FROM \"Indexers\" WHERE \"Implementation\" = 'Omgwtfnzbs'");
        }
    }
}
