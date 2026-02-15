using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(153)]
    public class add_on_episodefiledelete_for_upgrade : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Notifications").AddColumn("OnEpisodeFileDeleteForUpgrade").AsBoolean().WithDefaultValue(true);
        }
    }
}
