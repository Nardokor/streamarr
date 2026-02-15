using System.IO;
using NUnit.Framework;
using Streamarr.Core.Datastore.Migration.Framework;
using Streamarr.Test.Common.Datastore;

namespace Streamarr.Core.Test
{
    [SetUpFixture]
    public class RemoveCachedDatabase
    {
        [OneTimeSetUp]
        [OneTimeTearDown]
        public void ClearCachedDatabase()
        {
            var mainCache = SqliteDatabase.GetCachedDb(MigrationType.Main);
            if (File.Exists(mainCache))
            {
                File.Delete(mainCache);
            }

            var logCache = SqliteDatabase.GetCachedDb(MigrationType.Log);
            if (File.Exists(logCache))
            {
                File.Delete(logCache);
            }
        }
    }
}
