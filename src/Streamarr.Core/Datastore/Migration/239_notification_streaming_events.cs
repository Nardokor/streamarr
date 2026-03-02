using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(239)]
    public class notification_streaming_events : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Notifications")
                 .AddColumn("OnGrab").AsBoolean().NotNullable().WithDefaultValue(false)
                 .AddColumn("OnLiveStreamStart").AsBoolean().NotNullable().WithDefaultValue(false)
                 .AddColumn("OnLiveStreamEnd").AsBoolean().NotNullable().WithDefaultValue(false)
                 .AddColumn("OnChannelAdded").AsBoolean().NotNullable().WithDefaultValue(false);
        }
    }
}
