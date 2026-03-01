using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(232)]
    public class channel_filter_overhaul : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Add new filter columns
            Alter.Table("Channels")
                 .AddColumn("WatchedWords").AsString().NotNullable().WithDefaultValue(string.Empty)
                 .AddColumn("IgnoredWords").AsString().NotNullable().WithDefaultValue(string.Empty)
                 .AddColumn("WatchedDefeatsIgnored").AsBoolean().NotNullable().WithDefaultValue(true)
                 .AddColumn("AutoDownload").AsBoolean().NotNullable().WithDefaultValue(true)
                 .AddColumn("DownloadVods").AsBoolean().NotNullable().WithDefaultValue(true)
                 .AddColumn("DownloadLive").AsBoolean().NotNullable().WithDefaultValue(false)
                 .AddColumn("RetentionVideos").AsBoolean().NotNullable().WithDefaultValue(false)
                 .AddColumn("RetentionShorts").AsBoolean().NotNullable().WithDefaultValue(false)
                 .AddColumn("RetentionVods").AsBoolean().NotNullable().WithDefaultValue(false)
                 .AddColumn("RetentionLive").AsBoolean().NotNullable().WithDefaultValue(false);

            // Migrate data from old columns before dropping them
            Execute.Sql("UPDATE \"Channels\" SET \"WatchedWords\" = COALESCE(\"TitleFilter\", '')");
            Execute.Sql("UPDATE \"Channels\" SET \"DownloadVods\" = \"DownloadLivestreams\"");
            Execute.Sql("UPDATE \"Channels\" SET \"DownloadLive\" = \"RecordLiveOnly\"");

            // Drop replaced columns
            Delete.Column("TitleFilter").FromTable("Channels");
            Delete.Column("PriorityFilter").FromTable("Channels");
            Delete.Column("DownloadLivestreams").FromTable("Channels");
            Delete.Column("RecordLiveOnly").FromTable("Channels");
        }
    }
}
