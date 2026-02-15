using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(55)]
    public class drop_old_profile_columns : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("QualityProfileId").FromTable("Series");
        }
    }
}
