using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.MetadataSource.Twitch;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.MetadataSource
{
    [TestFixture]
    public class TwitchFixture : CoreTest<Twitch>
    {
        [SetUp]
        public void SetUp()
        {
            Subject.Definition = new MetadataSourceDefinition
            {
                Settings = new TwitchSettings
                {
                    ClientId = "test-client-id",
                    ClientSecret = "test-client-secret",
                }
            };
        }

        // ── GetDownloadUrl ────────────────────────────────────────────────────

        [Test]
        public void get_download_url_should_build_vod_url_for_numeric_id()
        {
            Subject.GetDownloadUrl("1234567890")
                   .Should().Be("https://www.twitch.tv/videos/1234567890");
        }

        [Test]
        public void get_download_url_should_strip_live_prefix_and_build_channel_url()
        {
            Subject.GetDownloadUrl("live:shroud")
                   .Should().Be("https://www.twitch.tv/shroud");
        }

        [Test]
        public void get_download_url_should_return_https_content_id_unchanged()
        {
            const string url = "https://www.twitch.tv/videos/9876543210";

            Subject.GetDownloadUrl(url).Should().Be(url);
        }
    }
}
