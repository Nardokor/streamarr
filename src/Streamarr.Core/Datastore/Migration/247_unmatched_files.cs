using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(247)]
    public class unmatched_files : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("UnmatchedFiles")
                  .WithColumn("CreatorId").AsInt32().NotNullable()
                  .WithColumn("FilePath").AsString().NotNullable()
                  .WithColumn("FileName").AsString().NotNullable()
                  .WithColumn("FileSize").AsInt64().NotNullable()
                  .WithColumn("DateFound").AsDateTime().NotNullable()
                  .WithColumn("Reason").AsInt32().NotNullable();
        }
    }
}
