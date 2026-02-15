using System.Data.SQLite;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Common.Disk;
using Streamarr.Core.Datastore.Converters;
using Streamarr.Core.Test.Framework;
using Streamarr.Test.Common;

namespace Streamarr.Core.Test.Datastore.Converters
{
    [TestFixture]
    public class OsPathConverterFixture : CoreTest<OsPathConverter>
    {
        private SQLiteParameter _param;

        [SetUp]
        public void Setup()
        {
            _param = new SQLiteParameter();
        }

        [Test]
        public void should_return_string_when_saving_os_path_to_db()
        {
            var path = @"C:\Test\TV".AsOsAgnostic();
            var osPath = new OsPath(path);

            Subject.SetValue(_param, osPath);
            _param.Value.Should().Be(path);
        }

        [Test]
        public void should_return_os_path_when_getting_string_from_db()
        {
            var path = @"C:\Test\TV".AsOsAgnostic();
            var osPath = new OsPath(path);

            Subject.Parse(path).Should().Be(osPath);
        }

        [Test]
        public void should_return_empty_for_null_value_when_getting_from_db()
        {
            Subject.Parse(null).IsEmpty.Should().BeTrue();
        }
    }
}
