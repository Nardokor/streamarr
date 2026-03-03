using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(240)]
    public class members_content_flags : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Contents")
                 .AddColumn("IsMembers").AsBoolean().NotNullable().WithDefaultValue(false)
                 .AddColumn("IsAccessible").AsBoolean().NotNullable().WithDefaultValue(true);

            Alter.Table("Channels")
                 .AddColumn("DownloadMembers").AsBoolean().NotNullable().WithDefaultValue(false);
        }
    }
}
