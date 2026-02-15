using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(41)]
    public class fix_xbmc_season_images_metadata : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE \"MetadataFiles\" SET \"Type\" = 4 WHERE \"Consumer\" = 'XbmcMetadata' AND \"SeasonNumber\" IS NOT NULL");
        }
    }
}
