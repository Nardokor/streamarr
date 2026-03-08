using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(241)]
    public class content_previous_status : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Contents")
                 .AddColumn("PreviousStatus").AsInt32().Nullable();
        }
    }
}
