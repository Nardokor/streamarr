using System.IO;
using NUnit.Framework;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Test.Common.Datastore
{
    public static class SqliteDatabase
    {
        public static string GetCachedDb(MigrationType type)
        {
            return Path.Combine(TestContext.CurrentContext.TestDirectory, $"cached_{type}.db");
        }
    }
}
