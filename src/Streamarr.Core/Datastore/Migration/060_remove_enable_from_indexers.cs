using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(60)]
    public class remove_enable_from_indexers : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("Enable").FromTable("Indexers");
            Delete.Column("Protocol").FromTable("DownloadClients");
        }
    }
}
