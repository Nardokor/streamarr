using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Test.Framework;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.Test.ThingiProviderTests
{
    [TestFixture]
    public class NullConfigFixture : CoreTest<NullConfig>
    {
        [Test]
        public void should_be_valid()
        {
            Subject.Validate().IsValid.Should().BeTrue();
        }
    }
}
