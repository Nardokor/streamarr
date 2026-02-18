using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(225)]
    public class streamarr_initial : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("Creators")
                  .WithColumn("Title").AsString().NotNullable()
                  .WithColumn("CleanTitle").AsString().NotNullable()
                  .WithColumn("SortTitle").AsString().NotNullable()
                  .WithColumn("Description").AsString().Nullable()
                  .WithColumn("ThumbnailUrl").AsString().Nullable()
                  .WithColumn("Path").AsString().NotNullable()
                  .WithColumn("QualityProfileId").AsInt32()
                  .WithColumn("Tags").AsString().WithDefaultValue("[]")
                  .WithColumn("Monitored").AsBoolean().WithDefaultValue(true)
                  .WithColumn("Status").AsInt32().WithDefaultValue(0)
                  .WithColumn("Added").AsDateTime()
                  .WithColumn("LastInfoSync").AsDateTime().Nullable();

            Create.TableForModel("Channels")
                  .WithColumn("CreatorId").AsInt32().NotNullable()
                  .WithColumn("Platform").AsInt32().NotNullable()
                  .WithColumn("PlatformId").AsString().NotNullable()
                  .WithColumn("PlatformUrl").AsString().Nullable()
                  .WithColumn("Title").AsString().NotNullable()
                  .WithColumn("Description").AsString().Nullable()
                  .WithColumn("ThumbnailUrl").AsString().Nullable()
                  .WithColumn("Monitored").AsBoolean().WithDefaultValue(true)
                  .WithColumn("Status").AsInt32().WithDefaultValue(0)
                  .WithColumn("LastInfoSync").AsDateTime().Nullable();

            Create.TableForModel("Contents")
                  .WithColumn("ChannelId").AsInt32().NotNullable()
                  .WithColumn("ContentFileId").AsInt32().WithDefaultValue(0)
                  .WithColumn("PlatformContentId").AsString().NotNullable()
                  .WithColumn("ContentType").AsInt32().WithDefaultValue(0)
                  .WithColumn("Title").AsString().NotNullable()
                  .WithColumn("Description").AsString().Nullable()
                  .WithColumn("ThumbnailUrl").AsString().Nullable()
                  .WithColumn("Duration").AsString().Nullable()
                  .WithColumn("AirDateUtc").AsDateTime().Nullable()
                  .WithColumn("DateAdded").AsDateTime()
                  .WithColumn("Monitored").AsBoolean().WithDefaultValue(true)
                  .WithColumn("Status").AsInt32().WithDefaultValue(0);

            Create.TableForModel("ContentFiles")
                  .WithColumn("ContentId").AsInt32().NotNullable()
                  .WithColumn("RelativePath").AsString().NotNullable()
                  .WithColumn("Size").AsInt64()
                  .WithColumn("DateAdded").AsDateTime()
                  .WithColumn("Quality").AsString().WithDefaultValue("{}")
                  .WithColumn("OriginalFilePath").AsString().Nullable();

            // Indexes
            Create.Index().OnTable("Creators").OnColumn("CleanTitle");
            Create.Index().OnTable("Creators").OnColumn("Path").Unique();

            Create.Index().OnTable("Channels").OnColumn("CreatorId");
            Create.Index().OnTable("Channels").OnColumn("PlatformId");

            Create.Index().OnTable("Contents").OnColumn("ChannelId");
            Create.Index().OnTable("Contents").OnColumn("PlatformContentId");
            Create.Index().OnTable("Contents").OnColumn("ContentFileId");

            Create.Index().OnTable("ContentFiles").OnColumn("ContentId");
        }
    }
}
