using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Test.Framework;
using ContentEntity = Streamarr.Core.Content.Content;

namespace Streamarr.Core.Test.Content
{
    [TestFixture]
    public class ContentFilterServiceFixture : CoreTest<ContentFilterService>
    {
        private Channel _channel;

        [SetUp]
        public void SetUp()
        {
            // Permissive defaults: all types enabled, no word filters
            _channel = new Channel
            {
                Id = 1,
                DownloadVideos = true,
                DownloadShorts = true,
                DownloadVods = true,
                DownloadLive = true,
                WatchedWords = string.Empty,
                IgnoredWords = string.Empty,
                WatchedDefeatsIgnored = true,
            };
        }

        // ── Content-type gate ─────────────────────────────────────────────────

        [Test]
        public void should_pass_video_when_download_videos_enabled()
        {
            Subject.PassesFilter("My Video", ContentType.Video, _channel).Should().BeTrue();
        }

        [Test]
        public void should_block_video_when_download_videos_disabled()
        {
            _channel.DownloadVideos = false;

            Subject.PassesFilter("My Video", ContentType.Video, _channel).Should().BeFalse();
        }

        [Test]
        public void should_block_short_when_download_shorts_disabled()
        {
            _channel.DownloadShorts = false;

            Subject.PassesFilter("My Short", ContentType.Short, _channel).Should().BeFalse();
        }

        [Test]
        public void should_block_vod_when_download_vods_disabled()
        {
            _channel.DownloadVods = false;

            Subject.PassesFilter("My VoD", ContentType.Vod, _channel).Should().BeFalse();
        }

        [Test]
        public void should_block_live_and_upcoming_when_download_live_disabled()
        {
            _channel.DownloadLive = false;

            Subject.PassesFilter("My Stream", ContentType.Live, _channel).Should().BeFalse();
            Subject.PassesFilter("Upcoming Stream", ContentType.Upcoming, _channel).Should().BeFalse();
        }

        // ── No word filters (everything passes) ───────────────────────────────

        [Test]
        public void should_pass_any_title_when_no_word_filters_set()
        {
            Subject.PassesFilter("Totally random title", ContentType.Video, _channel).Should().BeTrue();
        }

        [Test]
        public void should_treat_null_title_as_empty_string()
        {
            _channel.IgnoredWords = "gaming";

            Subject.PassesFilter(null, ContentType.Video, _channel).Should().BeTrue();
        }

        // ── Watched words (whitelist) ─────────────────────────────────────────

        [Test]
        public void should_pass_title_matching_watched_word()
        {
            _channel.WatchedWords = "asmr";

            Subject.PassesFilter("ASMR cooking video", ContentType.Video, _channel).Should().BeTrue();
        }

        [Test]
        public void should_block_title_not_matching_watched_word()
        {
            _channel.WatchedWords = "asmr";

            Subject.PassesFilter("Gaming highlights", ContentType.Video, _channel).Should().BeFalse();
        }

        [Test]
        public void should_match_watched_words_case_insensitively()
        {
            _channel.WatchedWords = "ASMR";

            Subject.PassesFilter("asmr tapping", ContentType.Video, _channel).Should().BeTrue();
        }

        // ── Ignored words ─────────────────────────────────────────────────────

        [Test]
        public void should_pass_title_not_matching_ignored_word()
        {
            _channel.IgnoredWords = "sponsored";

            Subject.PassesFilter("Great cooking video", ContentType.Video, _channel).Should().BeTrue();
        }

        [Test]
        public void should_block_title_matching_ignored_word()
        {
            _channel.IgnoredWords = "sponsored";

            Subject.PassesFilter("Sponsored cooking video", ContentType.Video, _channel).Should().BeFalse();
        }

        // ── WatchedDefeatsIgnored ─────────────────────────────────────────────

        [Test]
        public void should_pass_when_watched_defeats_ignored_and_title_matches_both()
        {
            _channel.WatchedWords = "asmr";
            _channel.IgnoredWords = "sponsored";
            _channel.WatchedDefeatsIgnored = true;

            Subject.PassesFilter("ASMR sponsored session", ContentType.Video, _channel).Should().BeTrue();
        }

        [Test]
        public void should_block_when_watched_defeats_ignored_false_and_title_matches_both()
        {
            _channel.WatchedWords = "asmr";
            _channel.IgnoredWords = "sponsored";
            _channel.WatchedDefeatsIgnored = false;

            Subject.PassesFilter("ASMR sponsored session", ContentType.Video, _channel).Should().BeFalse();
        }

        [Test]
        public void should_block_when_title_matches_ignored_but_not_watched()
        {
            _channel.WatchedWords = "asmr";
            _channel.IgnoredWords = "sponsored";
            _channel.WatchedDefeatsIgnored = true;

            // WatchedDefeatsIgnored only rescues items that are *explicitly* watched
            Subject.PassesFilter("Regular sponsored video", ContentType.Video, _channel).Should().BeFalse();
        }

        // ── ReapplyFilterForChannel ────────────────────────────────────────────

        [Test]
        public void reapply_should_promote_unwanted_to_missing_when_filter_now_passes()
        {
            // Channel now has no word filter so everything passes
            var content = new ContentEntity
            {
                Id = 1,
                Title = "Good video",
                ContentType = ContentType.Video,
                Status = ContentStatus.Unwanted,
            };

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.GetByChannelId(_channel.Id))
                  .Returns(new List<ContentEntity> { content });

            Subject.ReapplyFilterForChannel(_channel);

            content.Status.Should().Be(ContentStatus.Missing);
            Mocker.GetMock<IContentService>()
                  .Verify(s => s.UpdateContent(content), Times.Once);
        }

        [Test]
        public void reapply_should_demote_missing_to_unwanted_when_filter_now_fails()
        {
            _channel.DownloadShorts = false;

            var content = new ContentEntity
            {
                Id = 2,
                Title = "A short",
                ContentType = ContentType.Short,
                Status = ContentStatus.Missing,
            };

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.GetByChannelId(_channel.Id))
                  .Returns(new List<ContentEntity> { content });

            Subject.ReapplyFilterForChannel(_channel);

            content.Status.Should().Be(ContentStatus.Unwanted);
        }

        [Test]
        public void reapply_should_not_touch_downloaded_content()
        {
            var content = new ContentEntity
            {
                Id = 3,
                Title = "Already downloaded",
                ContentType = ContentType.Video,
                Status = ContentStatus.Downloaded,
            };

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.GetByChannelId(_channel.Id))
                  .Returns(new List<ContentEntity> { content });

            Subject.ReapplyFilterForChannel(_channel);

            Mocker.GetMock<IContentService>()
                  .Verify(s => s.UpdateContent(It.IsAny<ContentEntity>()), Times.Never);
        }
    }
}
