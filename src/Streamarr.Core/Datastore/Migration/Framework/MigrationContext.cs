using System;

namespace Streamarr.Core.Datastore.Migration.Framework
{
    public class MigrationContext
    {
        public static MigrationContext Current { get; set; }

        public MigrationType MigrationType { get; private set; }
        public long? DesiredVersion { get; set; }

        public Action<StreamarrMigrationBase> BeforeMigration { get; set; }

        public MigrationContext(MigrationType migrationType, long? desiredVersion = null)
        {
            MigrationType = migrationType;
            DesiredVersion = desiredVersion;
        }
    }
}
