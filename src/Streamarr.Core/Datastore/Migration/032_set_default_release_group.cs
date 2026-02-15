using System;
using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(32)]
    public class set_default_release_group : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Update.Table("EpisodeFiles").Set(new { ReleaseGroup = "DRONE" }).Where(new { ReleaseGroup = DBNull.Value });
        }
    }
}
