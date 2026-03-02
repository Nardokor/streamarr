using System;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.MetadataSource.YouTube;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.MetadataSource
{
    [TestFixture]
    public class YouTubeApiClientFixture : CoreTest<YouTubeApiClient>
    {
        // ── NormalizeThumbnailUrl ─────────────────────────────────────────────

        [Test]
        public void normalize_should_return_empty_for_empty_input()
        {
            YouTubeApiClient.NormalizeThumbnailUrl(string.Empty).Should().BeEmpty();
        }

        [Test]
        public void normalize_should_replace_legacy_ggpht_domain()
        {
            var url = "https://yt3.ggpht.com/photo.jpg";

            var result = YouTubeApiClient.NormalizeThumbnailUrl(url);

            result.Should().Contain("yt3.googleusercontent.com");
            result.Should().NotContain("yt3.ggpht.com");
        }

        [Test]
        public void normalize_should_clamp_size_parameter_to_s160()
        {
            var url = "https://yt3.googleusercontent.com/photo.jpg=s800";

            var result = YouTubeApiClient.NormalizeThumbnailUrl(url);

            result.Should().EndWith("=s160");
        }

        [Test]
        public void normalize_should_replace_domain_and_clamp_size_together()
        {
            var url = "https://yt3.ggpht.com/photo.jpg=s240";

            var result = YouTubeApiClient.NormalizeThumbnailUrl(url);

            result.Should().Contain("yt3.googleusercontent.com");
            result.Should().EndWith("=s160");
        }

        [Test]
        public void normalize_should_leave_url_without_size_param_unchanged()
        {
            var url = "https://yt3.googleusercontent.com/photo.jpg";

            YouTubeApiClient.NormalizeThumbnailUrl(url).Should().Be(url);
        }

        [Test]
        public void normalize_should_handle_case_insensitive_domain_replacement()
        {
            var url = "https://YT3.GGPHT.COM/photo.jpg";

            var result = YouTubeApiClient.NormalizeThumbnailUrl(url);

            result.Should().Contain("yt3.googleusercontent.com");
        }

        // ── API key guards ────────────────────────────────────────────────────

        [Test]
        public void get_playlist_items_should_throw_when_api_key_is_empty()
        {
            Action act = () => Subject.GetPlaylistItems(string.Empty, "UUtest");

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*API key*");
        }

        [Test]
        public void get_playlist_items_should_throw_when_api_key_is_whitespace()
        {
            Action act = () => Subject.GetPlaylistItems("   ", "UUtest");

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void get_video_details_should_throw_when_api_key_is_empty()
        {
            Action act = () => Subject.GetVideoDetails(string.Empty, new[] { "abc123" });

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*API key*");
        }

        [Test]
        public void get_video_details_should_throw_when_api_key_is_whitespace()
        {
            Action act = () => Subject.GetVideoDetails("  ", new[] { "abc123" });

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void get_channel_thumbnail_url_should_return_empty_when_api_key_is_empty()
        {
            var result = Subject.GetChannelThumbnailUrl(string.Empty, "UCtest123");

            result.Should().BeEmpty();
        }

        [Test]
        public void get_channel_thumbnail_url_should_return_empty_when_api_key_is_null()
        {
            var result = Subject.GetChannelThumbnailUrl(null, "UCtest123");

            result.Should().BeEmpty();
        }
    }
}
