using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(14)]
    public class drop_air_date : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("AirDate").FromTable("Episodes");
        }
    }
}
