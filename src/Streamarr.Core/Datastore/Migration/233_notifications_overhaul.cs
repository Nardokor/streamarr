using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(233)]
    public class notifications_overhaul : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Recreate Notifications table with only the columns Streamarr needs.
            // The inherited Sonarr table has many NOT NULL columns (OnGrab, OnUpgrade,
            // OnRename, etc.) that have no defaults and are not part of NotificationDefinition,
            // which would cause constraint violations on INSERT.

            Rename.Table("Notifications").To("Notifications_bak");

            Create.TableForModel("Notifications")
                  .WithColumn("Name").AsString().NotNullable()
                  .WithColumn("Enable").AsBoolean().NotNullable().WithDefaultValue(true)
                  .WithColumn("OnDownload").AsBoolean().NotNullable().WithDefaultValue(false)
                  .WithColumn("Implementation").AsString().NotNullable()
                  .WithColumn("ConfigContract").AsString().Nullable()
                  .WithColumn("Settings").AsString().Nullable()
                  .WithColumn("Tags").AsString().NotNullable().WithDefaultValue("[]");

            Execute.Sql(
                "INSERT INTO \"Notifications\" (\"Id\", \"Name\", \"Enable\", \"OnDownload\", \"Implementation\", \"ConfigContract\", \"Settings\", \"Tags\") " +
                "SELECT \"Id\", \"Name\", 1, \"OnDownload\", \"Implementation\", \"ConfigContract\", \"Settings\", COALESCE(\"Tags\", '[]') " +
                "FROM \"Notifications_bak\"");

            Delete.Table("Notifications_bak");
        }
    }
}
