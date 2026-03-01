using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(235)]
    public class Migration235MetadataSources : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Create MetadataSources table
            Create.TableForModel("MetadataSources")
                  .WithColumn("Name").AsString().NotNullable()
                  .WithColumn("Implementation").AsString().NotNullable()
                  .WithColumn("ConfigContract").AsString().Nullable()
                  .WithColumn("Settings").AsString().Nullable()
                  .WithColumn("Enable").AsBoolean().NotNullable().WithDefaultValue(true)
                  .WithColumn("Tags").AsString().NotNullable().WithDefaultValue("[]")
                  .WithColumn("Platform").AsInt32().NotNullable().WithDefaultValue(0);

            // Remove legacy YouTube config keys (settings are now managed via the MetadataSources UI)
            // Sources are not auto-seeded; users add them manually through the Sources settings page.
            Execute.Sql(@"
                DELETE FROM Config WHERE Key IN (
                    'YouTubeApiKey',
                    'YouTubeFullRefreshIntervalHours',
                    'YouTubeLiveCheckIntervalMinutes',
                    'YouTubeDefaultDownloadVideos',
                    'YouTubeDefaultDownloadShorts',
                    'YouTubeDefaultDownloadVods',
                    'YouTubeDefaultDownloadLive',
                    'YouTubeDefaultWatchedWords',
                    'YouTubeDefaultIgnoredWords',
                    'YouTubeDefaultWatchedDefeatsIgnored',
                    'YouTubeDefaultAutoDownload'
                )
            ");
        }
    }
}
