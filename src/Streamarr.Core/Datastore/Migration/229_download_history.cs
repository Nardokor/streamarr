using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(229)]
    public class download_history : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Table("DownloadHistory");

            Create.TableForModel("DownloadHistory")
                  .WithColumn("ContentId").AsInt32().NotNullable()
                  .WithColumn("ChannelId").AsInt32().NotNullable()
                  .WithColumn("CreatorId").AsInt32().NotNullable()
                  .WithColumn("Title").AsString().NotNullable()
                  .WithColumn("Quality").AsString().WithDefaultValue("{}")
                  .WithColumn("EventType").AsInt32().NotNullable()
                  .WithColumn("Data").AsString().NotNullable().WithDefaultValue(string.Empty)
                  .WithColumn("Date").AsDateTime().NotNullable();

            Create.Index().OnTable("DownloadHistory").OnColumn("CreatorId");
            Create.Index().OnTable("DownloadHistory").OnColumn("ContentId");
            Create.Index().OnTable("DownloadHistory").OnColumn("Date");
        }
    }
}
