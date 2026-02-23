using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(230)]
    public class channel_record_live_only : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Channels")
                 .AddColumn("RecordLiveOnly")
                 .AsBoolean()
                 .NotNullable()
                 .WithDefaultValue(false);
        }
    }
}
