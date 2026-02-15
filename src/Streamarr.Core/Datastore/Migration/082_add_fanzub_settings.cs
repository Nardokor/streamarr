using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(82)]
    public class add_fanzub_settings : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Update.Table("Indexers").Set(new { ConfigContract = "FanzubSettings" }).Where(new { Implementation = "Fanzub", ConfigContract = "NullConfig" });
        }
    }
}
