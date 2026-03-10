using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(243)]
    public class retention_keep_flags : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Add new "always keep" flag columns (inverted from the old "apply retention to" columns)
            Alter.Table("Channels")
                 .AddColumn("KeepVideos").AsBoolean().NotNullable().WithDefaultValue(true)
                 .AddColumn("KeepShorts").AsBoolean().NotNullable().WithDefaultValue(true)
                 .AddColumn("KeepVods").AsBoolean().NotNullable().WithDefaultValue(false)
                 .AddColumn("KeepMembers").AsBoolean().NotNullable().WithDefaultValue(false)
                 .AddColumn("RetentionKeepWords").AsString().NotNullable().WithDefaultValue(string.Empty);

            // Invert old "apply retention to" flags — if retention was NOT applied, the type was kept
            Execute.Sql("UPDATE \"Channels\" SET \"KeepVideos\" = CASE WHEN \"RetentionVideos\" = 0 THEN 1 ELSE 0 END");
            Execute.Sql("UPDATE \"Channels\" SET \"KeepShorts\" = CASE WHEN \"RetentionShorts\" = 0 THEN 1 ELSE 0 END");
            Execute.Sql("UPDATE \"Channels\" SET \"KeepVods\" = CASE WHEN \"RetentionVods\" = 0 THEN 1 ELSE 0 END");
            Execute.Sql("UPDATE \"Channels\" SET \"KeepMembers\" = CASE WHEN \"RetentionMembers\" = 0 THEN 1 ELSE 0 END");

            // Migrate watched words → keep words
            Execute.Sql("UPDATE \"Channels\" SET \"RetentionKeepWords\" = \"RetentionWatchedWords\" WHERE \"RetentionWatchedWords\" != ''");

            // Drop superseded columns
            Delete.Column("RetentionVideos").FromTable("Channels");
            Delete.Column("RetentionShorts").FromTable("Channels");
            Delete.Column("RetentionVods").FromTable("Channels");
            Delete.Column("RetentionLive").FromTable("Channels");
            Delete.Column("RetentionMembers").FromTable("Channels");
            Delete.Column("RetentionWatchedWords").FromTable("Channels");
            Delete.Column("RetentionIgnoredWords").FromTable("Channels");
        }
    }
}
