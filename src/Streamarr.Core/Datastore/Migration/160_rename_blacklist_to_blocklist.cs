using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(160)]
    public class rename_blacklist_to_blocklist : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Rename.Table("Blacklist").To("Blocklist");
        }
    }
}
