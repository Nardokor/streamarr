using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(92)]
    public class add_unverifiedscenenumbering : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Episodes").AddColumn("UnverifiedSceneNumbering").AsBoolean().WithDefaultValue(false);
        }
    }
}
