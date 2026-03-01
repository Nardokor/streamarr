using FluentAssertions;
using FluentValidation.Results;
using Moq;
using NUnit.Framework;
using Streamarr.Core.Channels;
using Streamarr.Core.HealthCheck;
using Streamarr.Core.HealthCheck.Checks;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.MetadataSource.YouTube;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.HealthCheck
{
    [TestFixture]
    public class YouTubeApiKeyHealthCheckFixture : CoreTest<YouTubeApiKeyHealthCheck>
    {
        private Mock<IMetadataSource> _sourceStub;

        [SetUp]
        public void SetUp()
        {
            _sourceStub = new Mock<IMetadataSource>();

            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns((IMetadataSource)null);
        }

        private void GivenYouTubeSource(string apiKey, ValidationResult testResult = null)
        {
            var settings = new YouTubeSettings { ApiKey = apiKey };
            var definition = new MetadataSourceDefinition
            {
                Name = "YouTube",
                Implementation = "YouTube",
                Settings = settings,
                Enable = true,
                Platform = PlatformType.YouTube
            };

            _sourceStub.SetupGet(s => s.Definition).Returns(definition);
            _sourceStub.Setup(s => s.Test()).Returns(testResult ?? new ValidationResult());

            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns(_sourceStub.Object);
        }

        [Test]
        public void should_return_ok_when_no_youtube_source_configured()
        {
            // factory returns null (default setup in SetUp)

            Subject.Check().Type.Should().Be(HealthCheckResult.Ok);
        }

        [Test]
        public void should_return_warning_when_api_key_is_empty()
        {
            GivenYouTubeSource(apiKey: string.Empty);

            var result = Subject.Check();

            result.Type.Should().Be(HealthCheckResult.Warning);
            result.Reason.Should().Be(HealthCheckReason.YouTubeApiKeyNotConfigured);
        }

        [Test]
        public void should_return_ok_when_api_key_is_valid_and_test_passes()
        {
            GivenYouTubeSource(apiKey: "AIzaSy_test_key", testResult: new ValidationResult());

            Subject.Check().Type.Should().Be(HealthCheckResult.Ok);
        }

        [Test]
        public void should_return_error_when_api_key_is_invalid()
        {
            var failures = new[] { new ValidationFailure("ApiKey", "API key is invalid") };
            GivenYouTubeSource(apiKey: "bad_key", testResult: new ValidationResult(failures));

            var result = Subject.Check();

            result.Type.Should().Be(HealthCheckResult.Error);
            result.Reason.Should().Be(HealthCheckReason.YouTubeApiKeyInvalid);
            result.Message.Should().Contain("API key is invalid");
        }
    }
}
