using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(212)]
    public class add_minium_upgrade_format_score_to_quality_profiles : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("QualityProfiles").AddColumn("MinUpgradeFormatScore").AsInt32().WithDefaultValue(1);
        }
    }
}
