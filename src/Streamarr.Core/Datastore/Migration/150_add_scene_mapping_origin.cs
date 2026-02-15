using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(150)]
    public class add_scene_mapping_origin : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("SceneMappings")
                .AddColumn("SceneOrigin").AsString().Nullable()
                .AddColumn("SearchMode").AsInt32().Nullable()
                .AddColumn("Comment").AsString().Nullable();
        }
    }
}
