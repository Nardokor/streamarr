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

            // Migrate existing YouTube settings from Config table into a MetadataSources row
            Execute.Sql(@"
                INSERT INTO MetadataSources (Name, Implementation, ConfigContract, Settings, Enable, Tags, Platform)
                SELECT
                    'YouTube',
                    'YouTube',
                    'YouTubeSettings',
                    json_object(
                        'apiKey',                      COALESCE((SELECT Value FROM Config WHERE Key='YouTubeApiKey'), ''),
                        'refreshIntervalHours',         COALESCE(CAST((SELECT Value FROM Config WHERE Key='YouTubeFullRefreshIntervalHours') AS INTEGER), 24),
                        'liveCheckIntervalMinutes',     COALESCE(CAST((SELECT Value FROM Config WHERE Key='YouTubeLiveCheckIntervalMinutes') AS INTEGER), 60),
                        'defaultDownloadVideos',        json(IIF(COALESCE(CAST((SELECT Value FROM Config WHERE Key='YouTubeDefaultDownloadVideos') AS INTEGER), 1), 'true', 'false')),
                        'defaultDownloadShorts',        json(IIF(COALESCE(CAST((SELECT Value FROM Config WHERE Key='YouTubeDefaultDownloadShorts') AS INTEGER), 1), 'true', 'false')),
                        'defaultDownloadVods',          json(IIF(COALESCE(CAST((SELECT Value FROM Config WHERE Key='YouTubeDefaultDownloadVods') AS INTEGER), 1), 'true', 'false')),
                        'defaultDownloadLive',          json(IIF(COALESCE(CAST((SELECT Value FROM Config WHERE Key='YouTubeDefaultDownloadLive') AS INTEGER), 0), 'true', 'false')),
                        'defaultWatchedWords',          COALESCE((SELECT Value FROM Config WHERE Key='YouTubeDefaultWatchedWords'), ''),
                        'defaultIgnoredWords',          COALESCE((SELECT Value FROM Config WHERE Key='YouTubeDefaultIgnoredWords'), ''),
                        'defaultWatchedDefeatsIgnored', json(IIF(COALESCE(CAST((SELECT Value FROM Config WHERE Key='YouTubeDefaultWatchedDefeatsIgnored') AS INTEGER), 1), 'true', 'false')),
                        'defaultAutoDownload',          json(IIF(COALESCE(CAST((SELECT Value FROM Config WHERE Key='YouTubeDefaultAutoDownload') AS INTEGER), 1), 'true', 'false'))
                    ),
                    1,
                    '[]',
                    1
            ");

            // Remove the now-migrated YouTube config keys
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
