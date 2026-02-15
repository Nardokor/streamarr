using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(63)]
    public class add_remotepathmappings : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("RemotePathMappings")
                  .WithColumn("Host").AsString()
                  .WithColumn("RemotePath").AsString()
                  .WithColumn("LocalPath").AsString();
        }
    }
}
