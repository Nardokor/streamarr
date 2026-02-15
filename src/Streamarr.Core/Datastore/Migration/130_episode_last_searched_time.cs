using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(130)]
    public class episode_last_searched_time : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Episodes").AddColumn("LastSearchTime").AsDateTime().Nullable();
        }
    }
}
