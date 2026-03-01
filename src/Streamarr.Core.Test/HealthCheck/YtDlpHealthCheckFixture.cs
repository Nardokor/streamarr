using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Download.YtDlp;
using Streamarr.Core.HealthCheck;
using Streamarr.Core.HealthCheck.Checks;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.HealthCheck
{
    [TestFixture]
    public class YtDlpHealthCheckFixture : CoreTest<YtDlpHealthCheck>
    {
        private void GivenYtDlpAvailable(bool available)
        {
            Mocker.GetMock<IYtDlpClient>()
                  .Setup(c => c.IsAvailable())
                  .Returns(available);
        }

        [Test]
        public void should_return_ok_when_ytdlp_is_available()
        {
            GivenYtDlpAvailable(true);

            Subject.Check().Type.Should().Be(HealthCheckResult.Ok);
        }

        [Test]
        public void should_return_error_when_ytdlp_is_not_available()
        {
            GivenYtDlpAvailable(false);

            var result = Subject.Check();

            result.Type.Should().Be(HealthCheckResult.Error);
            result.Reason.Should().Be(HealthCheckReason.YtDlpNotAvailable);
        }
    }
}
