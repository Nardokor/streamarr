using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(12)]
    public class remove_custom_start_date : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("CustomStartDate").FromTable("Series");
        }
    }
}
