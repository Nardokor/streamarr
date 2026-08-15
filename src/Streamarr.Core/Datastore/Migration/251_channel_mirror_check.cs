using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(251)]
    public class channel_mirror_check : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Channels")
                 .AddColumn("LastMirrorCheck").AsDateTime().Nullable()
                 .AddColumn("MirrorCheckIntervalDays").AsInt32().NotNullable().WithDefaultValue(1);
        }
    }
}
