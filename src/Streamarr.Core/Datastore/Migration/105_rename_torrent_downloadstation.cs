using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(105)]
    public class rename_torrent_downloadstation : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Update.Table("DownloadClients").Set(new { Implementation = "TorrentDownloadStation" }).Where(new { Implementation = "DownloadStation" });
        }
    }
}
