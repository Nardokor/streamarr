using FluentAssertions;
using Moq;
using NUnit.Framework;
using Streamarr.Common.Test.DiskTests;
using Streamarr.Mono.Disk;

namespace Streamarr.Mono.Test.DiskProviderTests
{
    [TestFixture]
    [Platform(Exclude = "Win")]
    public class FreeSpaceFixture : FreeSpaceFixtureBase<DiskProvider>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IProcMountProvider>(new ProcMountProvider(TestLogger));
            Mocker.GetMock<ISymbolicLinkResolver>()
                  .Setup(v => v.GetCompleteRealPath(It.IsAny<string>()))
                  .Returns<string>(s => s);
        }

        [Ignore("Docker")]
        [Test]
        public void should_be_able_to_check_space_on_ramdrive()
        {
            Subject.GetAvailableSpace("/run/").Should().NotBe(0);
        }
    }
}
