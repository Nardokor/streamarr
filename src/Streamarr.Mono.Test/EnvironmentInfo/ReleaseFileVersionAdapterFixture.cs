using FluentAssertions;
using NUnit.Framework;
using Streamarr.Common.Disk;
using Streamarr.Mono.Disk;
using Streamarr.Mono.EnvironmentInfo.VersionAdapters;
using Streamarr.Test.Common;

namespace Streamarr.Mono.Test.EnvironmentInfo
{
    [TestFixture]
    [Platform("Linux")]
    public class ReleaseFileVersionAdapterFixture : TestBase<ReleaseFileVersionAdapter>
    {
        [SetUp]
        public void Setup()
        {
            NotBsd();

            Mocker.SetConstant<IDiskProvider>(Mocker.Resolve<DiskProvider>());
        }

        [Test]
        public void should_get_version_info()
        {
            var info = Subject.Read();
            info.FullName.Should().NotBeNullOrWhiteSpace();
            info.Name.Should().NotBeNullOrWhiteSpace();
            info.Version.Should().NotBeNullOrWhiteSpace();
        }
    }
}
