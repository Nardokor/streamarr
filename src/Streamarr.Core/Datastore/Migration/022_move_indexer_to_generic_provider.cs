using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(22)]
    public class move_indexer_to_generic_provider : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Indexers").AddColumn("ConfigContract").AsString().Nullable();
        }
    }
}
