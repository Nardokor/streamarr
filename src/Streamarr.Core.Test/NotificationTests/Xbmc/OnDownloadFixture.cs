using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using Streamarr.Core.MediaFiles;
using Streamarr.Core.Notifications;
using Streamarr.Core.Notifications.Xbmc;
using Streamarr.Core.Test.Framework;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Test.NotificationTests.Xbmc
{
    [TestFixture]
    public class OnDownloadFixture : CoreTest<Notifications.Xbmc.Xbmc>
    {
        private DownloadMessage _downloadMessage;

        [SetUp]
        public void Setup()
        {
            var series = Builder<Series>.CreateNew()
                                        .Build();

            var episodeFile = Builder<EpisodeFile>.CreateNew()
                                                   .Build();

            _downloadMessage = Builder<DownloadMessage>.CreateNew()
                                                       .With(d => d.Series = series)
                                                       .With(d => d.EpisodeFile = episodeFile)
                                                       .With(d => d.OldFiles = new List<DeletedEpisodeFile>())
                                                       .Build();

            Subject.Definition = new NotificationDefinition();
            Subject.Definition.Settings = new XbmcSettings
                                          {
                                              Host = "localhost",
                                              UpdateLibrary = true
                                          };
        }

        private void GivenOldFiles()
        {
            _downloadMessage.OldFiles = Builder<DeletedEpisodeFile>
                .CreateListOfSize(1)
                .All()
                .WithFactory(() => new DeletedEpisodeFile(Builder<EpisodeFile>.CreateNew().Build(), null))
                .Build()
                .ToList();

            Subject.Definition.Settings = new XbmcSettings
                                          {
                                              Host = "localhost",
                                              UpdateLibrary = true,
                                              CleanLibrary = true
                                          };
        }

        [Test]
        public void should_not_clean_if_no_episode_was_replaced()
        {
            Subject.OnDownload(_downloadMessage);
            Subject.ProcessQueue();

            Mocker.GetMock<IXbmcService>().Verify(v => v.Clean(It.IsAny<XbmcSettings>()), Times.Never());
        }

        [Test]
        public void should_clean_if_episode_was_replaced()
        {
            GivenOldFiles();
            Subject.OnDownload(_downloadMessage);
            Subject.ProcessQueue();

            Mocker.GetMock<IXbmcService>().Verify(v => v.Clean(It.IsAny<XbmcSettings>()), Times.Once());
        }
    }
}
