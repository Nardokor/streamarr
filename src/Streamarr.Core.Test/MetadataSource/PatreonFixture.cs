using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.MetadataSource.Patreon;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.MetadataSource
{
    [TestFixture]
    public class PatreonFixture : CoreTest<Patreon>
    {
        [SetUp]
        public void SetUp()
        {
            Subject.Definition = new MetadataSourceDefinition
            {
                Settings = new PatreonSettings
                {
                    CookiesFilePath = "/tmp/cookies.txt",
                }
            };
        }

        // ── GetDownloadUrl ────────────────────────────────────────────────────

        [Test]
        public void get_download_url_should_build_patreon_posts_url()
        {
            Subject.GetDownloadUrl("123456789")
                   .Should().Be("https://www.patreon.com/posts/123456789");
        }
    }
}
