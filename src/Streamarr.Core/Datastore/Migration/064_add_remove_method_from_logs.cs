using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(64)]
    public class remove_method_from_logs : StreamarrMigrationBase
    {
        protected override void LogDbUpgrade()
        {
            Delete.Column("Method").FromTable("Logs");
        }
    }
}
