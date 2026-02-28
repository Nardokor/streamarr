using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(234)]
    public class retention_exception_words : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Channels")
                 .AddColumn("RetentionExceptionWords").AsString().NotNullable().WithDefaultValue(string.Empty);
        }
    }
}
