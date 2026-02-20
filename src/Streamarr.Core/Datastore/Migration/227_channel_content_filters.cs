using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(227)]
    public class channel_content_filters : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Channels")
                 .AddColumn("DownloadVideos").AsBoolean().NotNullable().WithDefaultValue(true)
                 .AddColumn("DownloadShorts").AsBoolean().NotNullable().WithDefaultValue(true)
                 .AddColumn("DownloadLivestreams").AsBoolean().NotNullable().WithDefaultValue(true)
                 .AddColumn("TitleFilter").AsString().NotNullable().WithDefaultValue(string.Empty);
        }
    }
}
