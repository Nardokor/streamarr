using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.MetadataSource.Fourthwall;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.MetadataSource
{
    [TestFixture]
    public class FourthwallFixture : CoreTest<Fourthwall>
    {
        [SetUp]
        public void SetUp()
        {
            Subject.Definition = new MetadataSourceDefinition
            {
                Settings = new FourthwallSettings
                {
                    CookiesFilePath = "/tmp/cookies.txt",
                }
            };
        }

        // ── GetDownloadUrl ────────────────────────────────────────────────────

        [Test]
        public void get_download_url_should_build_youtube_watch_url()
        {
            Subject.GetDownloadUrl("dQw4w9WgXcQ")
                   .Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        }
    }
}
