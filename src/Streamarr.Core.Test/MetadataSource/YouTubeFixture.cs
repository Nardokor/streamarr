using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Download.YtDlp;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.MetadataSource.YouTube;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.MetadataSource
{
    [TestFixture]
    public class YouTubeFixture : CoreTest<YouTube>
    {
        private const string TestApiKey = "test-api-key-123";
        private const string TestChannelId = "UCtest1234567890123456AB"; // UC + 22 chars = valid channel ID (24 total)

        [SetUp]
        public void SetUp()
        {
            Subject.Definition = new MetadataSourceDefinition
            {
                Settings = new YouTubeSettings { ApiKey = TestApiKey }
            };

            Mocker.GetMock<IYtDlpClient>()
                  .Setup(c => c.GetChannelInfo(It.IsAny<string>()))
                  .Returns(new YtDlpChannelInfo
                  {
                      Channel = "Test Channel",
                      ChannelId = TestChannelId,
                      ChannelUrl = "https://www.youtube.com/channel/" + TestChannelId,
                  });

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetChannelThumbnailUrl(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(string.Empty);
        }

        // ── SearchCreator URL routing ─────────────────────────────────────────

        [Test]
        public void search_creator_should_pass_full_youtube_url_directly()
        {
            var url = "https://www.youtube.com/@shroud";

            Subject.SearchCreator(url);

            Mocker.GetMock<IYtDlpClient>()
                  .Verify(c => c.GetChannelInfo(url), Times.Once);
        }

        [Test]
        public void search_creator_should_build_url_from_at_handle()
        {
            Subject.SearchCreator("@shroud");

            Mocker.GetMock<IYtDlpClient>()
                  .Verify(c => c.GetChannelInfo("https://www.youtube.com/@shroud"), Times.Once);
        }

        [Test]
        public void search_creator_should_build_channel_url_from_uc_id()
        {
            Subject.SearchCreator(TestChannelId);

            Mocker.GetMock<IYtDlpClient>()
                  .Verify(c => c.GetChannelInfo("https://www.youtube.com/channel/" + TestChannelId), Times.Once);
        }

        [Test]
        public void search_creator_should_try_at_handle_url_for_bare_word()
        {
            Subject.SearchCreator("shroud");

            Mocker.GetMock<IYtDlpClient>()
                  .Verify(c => c.GetChannelInfo("https://www.youtube.com/@shroud"), Times.Once);
        }

        [Test]
        public void search_creator_should_return_result_with_channel_name()
        {
            var result = Subject.SearchCreator("@shroud");

            result.Name.Should().Be("Test Channel");
            result.Channels.Should().HaveCount(1);
            result.Channels[0].Platform.Should().Be(PlatformType.YouTube);
            result.Channels[0].PlatformId.Should().Be(TestChannelId);
        }

        [Test]
        public void search_creator_should_supplement_thumbnail_from_api_when_api_key_configured()
        {
            var apiThumbnail = "https://yt3.googleusercontent.com/api_thumb.jpg";

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetChannelThumbnailUrl(TestApiKey, TestChannelId))
                  .Returns(apiThumbnail);

            var result = Subject.SearchCreator("@shroud");

            result.ThumbnailUrl.Should().Be(apiThumbnail);
            result.Channels[0].ThumbnailUrl.Should().Be(apiThumbnail);
        }

        // ── GetNewContent ─────────────────────────────────────────────────────

        [Test]
        public void get_new_content_should_throw_when_platform_id_does_not_start_with_uc()
        {
            Action act = () => Subject.GetNewContent("https://youtube.com", "invalid_id", null).ToList();

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*UCxxxxxxx*");
        }

        [Test]
        public void get_new_content_should_throw_when_platform_id_is_empty()
        {
            Action act = () => Subject.GetNewContent("https://youtube.com", string.Empty, null).ToList();

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void get_new_content_should_request_uploads_playlist_derived_from_channel_id()
        {
            var channelId = "UC_xyzABCDEFGHIJKLMNOP";
            var expectedPlaylistId = "UU_xyzABCDEFGHIJKLMNOP";

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetPlaylistItems(TestApiKey, expectedPlaylistId, null))
                  .Returns(new List<(string, DateTime)>());

            Subject.GetNewContent("https://youtube.com", channelId, null).ToList();

            Mocker.GetMock<IYouTubeApiClient>()
                  .Verify(c => c.GetPlaylistItems(TestApiKey, expectedPlaylistId, null), Times.Once);
        }

        [Test]
        public void get_new_content_should_return_empty_when_no_playlist_items()
        {
            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetPlaylistItems(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                  .Returns(new List<(string, DateTime)>());

            var result = Subject.GetNewContent("https://youtube.com", TestChannelId, null);

            result.Should().BeEmpty();
        }

        [Test]
        public void get_new_content_should_map_short_duration_video_to_short_type()
        {
            var publishedAt = DateTime.UtcNow.AddDays(-1);

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetPlaylistItems(TestApiKey, It.IsAny<string>(), null))
                  .Returns(new List<(string, DateTime)> { ("vid1", publishedAt) });

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetVideoDetails(TestApiKey, It.IsAny<IEnumerable<string>>()))
                  .Returns(new List<YoutubeVideo>
                  {
                      new YoutubeVideo
                      {
                          Id = "vid1",
                          Snippet = new YoutubeVideoSnippet { Title = "Short video", ChannelId = TestChannelId },
                          ContentDetails = new YoutubeVideoContentDetails { Duration = "PT30S" }
                      }
                  });

            var result = Subject.GetNewContent("https://youtube.com", TestChannelId, null).ToList();

            result.Should().HaveCount(1);
            result[0].ContentType.Should().Be(ContentType.Short);
        }

        [Test]
        public void get_new_content_should_map_long_duration_video_to_video_type()
        {
            var publishedAt = DateTime.UtcNow.AddDays(-1);

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetPlaylistItems(TestApiKey, It.IsAny<string>(), null))
                  .Returns(new List<(string, DateTime)> { ("vid1", publishedAt) });

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetVideoDetails(TestApiKey, It.IsAny<IEnumerable<string>>()))
                  .Returns(new List<YoutubeVideo>
                  {
                      new YoutubeVideo
                      {
                          Id = "vid1",
                          Snippet = new YoutubeVideoSnippet { Title = "Full video", ChannelId = TestChannelId },
                          ContentDetails = new YoutubeVideoContentDetails { Duration = "PT10M" }
                      }
                  });

            var result = Subject.GetNewContent("https://youtube.com", TestChannelId, null).ToList();

            result[0].ContentType.Should().Be(ContentType.Video);
        }

        [Test]
        public void get_new_content_should_map_active_livestream_to_live_type()
        {
            var publishedAt = DateTime.UtcNow.AddDays(-1);

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetPlaylistItems(TestApiKey, It.IsAny<string>(), null))
                  .Returns(new List<(string, DateTime)> { ("vid1", publishedAt) });

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetVideoDetails(TestApiKey, It.IsAny<IEnumerable<string>>()))
                  .Returns(new List<YoutubeVideo>
                  {
                      new YoutubeVideo
                      {
                          Id = "vid1",
                          Snippet = new YoutubeVideoSnippet { Title = "Live now", ChannelId = TestChannelId },
                          LiveStreamingDetails = new YoutubeVideoLiveStreamingDetails
                          {
                              ActualStartTime = DateTime.UtcNow.AddHours(-1),
                              ActualEndTime = null
                          }
                      }
                  });

            var result = Subject.GetNewContent("https://youtube.com", TestChannelId, null).ToList();

            result[0].ContentType.Should().Be(ContentType.Live);
        }

        [Test]
        public void get_new_content_should_map_finished_livestream_to_vod_type()
        {
            var publishedAt = DateTime.UtcNow.AddDays(-1);

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetPlaylistItems(TestApiKey, It.IsAny<string>(), null))
                  .Returns(new List<(string, DateTime)> { ("vid1", publishedAt) });

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetVideoDetails(TestApiKey, It.IsAny<IEnumerable<string>>()))
                  .Returns(new List<YoutubeVideo>
                  {
                      new YoutubeVideo
                      {
                          Id = "vid1",
                          Snippet = new YoutubeVideoSnippet { Title = "Stream VOD", ChannelId = TestChannelId },
                          LiveStreamingDetails = new YoutubeVideoLiveStreamingDetails
                          {
                              ActualStartTime = DateTime.UtcNow.AddDays(-1),
                              ActualEndTime = DateTime.UtcNow.AddDays(-1).AddHours(2)
                          }
                      }
                  });

            var result = Subject.GetNewContent("https://youtube.com", TestChannelId, null).ToList();

            result[0].ContentType.Should().Be(ContentType.Vod);
        }

        [Test]
        public void get_new_content_should_map_scheduled_livestream_to_upcoming_type()
        {
            var publishedAt = DateTime.UtcNow.AddDays(-1);

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetPlaylistItems(TestApiKey, It.IsAny<string>(), null))
                  .Returns(new List<(string, DateTime)> { ("vid1", publishedAt) });

            Mocker.GetMock<IYouTubeApiClient>()
                  .Setup(c => c.GetVideoDetails(TestApiKey, It.IsAny<IEnumerable<string>>()))
                  .Returns(new List<YoutubeVideo>
                  {
                      new YoutubeVideo
                      {
                          Id = "vid1",
                          Snippet = new YoutubeVideoSnippet { Title = "Upcoming stream", ChannelId = TestChannelId },
                          LiveStreamingDetails = new YoutubeVideoLiveStreamingDetails
                          {
                              ScheduledStartTime = DateTime.UtcNow.AddDays(1)
                          }
                      }
                  });

            var result = Subject.GetNewContent("https://youtube.com", TestChannelId, null).ToList();

            result[0].ContentType.Should().Be(ContentType.Upcoming);
        }

        // ── GetLivestreamStatusUpdates ────────────────────────────────────────

        [Test]
        public void get_livestream_status_updates_should_return_empty_for_no_ids()
        {
            var result = Subject.GetLivestreamStatusUpdates(new List<string>());

            result.Should().BeEmpty();
        }

        [Test]
        public void get_livestream_status_updates_should_return_empty_when_api_key_not_configured()
        {
            Subject.Definition = new MetadataSourceDefinition
            {
                Settings = new YouTubeSettings { ApiKey = string.Empty }
            };

            var result = Subject.GetLivestreamStatusUpdates(new[] { "vid1" });

            result.Should().BeEmpty();
        }
    }
}
