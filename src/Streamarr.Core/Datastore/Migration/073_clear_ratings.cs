using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(73)]
    public class clear_ratings : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Update.Table("Series")
                  .Set(new { Ratings = "{}" })
                  .AllRows();

            Update.Table("Episodes")
                  .Set(new { Ratings = "{}" })
                  .AllRows();
        }
    }
}
