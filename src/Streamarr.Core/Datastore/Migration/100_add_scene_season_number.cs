using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(100)]
    public class add_scene_season_number : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("SceneMappings").AlterColumn("SeasonNumber").AsInt32().Nullable();
            Alter.Table("SceneMappings").AddColumn("SceneSeasonNumber").AsInt32().Nullable();
        }
    }
}
