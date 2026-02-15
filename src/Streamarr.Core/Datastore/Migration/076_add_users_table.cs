using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(76)]
    public class add_users_table : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("Users")
                  .WithColumn("Identifier").AsString().NotNullable().Unique()
                  .WithColumn("Username").AsString().NotNullable().Unique()
                  .WithColumn("Password").AsString().NotNullable();
        }
    }
}
