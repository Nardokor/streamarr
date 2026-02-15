using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(124)]
    public class remove_media_browser_metadata : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.FromTable("Metadata").Row(new { Implementation = "MediaBrowserMetadata" });
            Delete.FromTable("MetadataFiles").Row(new { Consumer = "MediaBrowserMetadata" });
        }
    }
}
