using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(48)]
    public class add_title_to_scenemappings : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("SceneMappings").AddColumn("Title").AsString().Nullable();
        }
    }
}
