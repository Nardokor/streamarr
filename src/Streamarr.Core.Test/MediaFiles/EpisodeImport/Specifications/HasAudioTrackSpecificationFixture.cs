using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.MediaFiles.EpisodeImport.Specifications;
using Streamarr.Core.MediaFiles.MediaInfo;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Test.Framework;
using Streamarr.Core.Tv;
using Streamarr.Test.Common;

namespace Streamarr.Core.Test.MediaFiles.EpisodeImport.Specifications
{
    [TestFixture]
    public class HasAudioTrackSpecificationFixture : CoreTest<HasAudioTrackSpecification>
    {
        private Series _series;
        private LocalEpisode _localEpisode;
        private string _rootFolder;

        [SetUp]
        public void Setup()
        {
             _rootFolder = @"C:\Test\TV".AsOsAgnostic();

             _series = Builder<Series>.CreateNew()
                                     .With(s => s.SeriesType = SeriesTypes.Standard)
                                     .With(s => s.Path = Path.Combine(_rootFolder, "30 Rock"))
                                     .Build();

             var episodes = Builder<Episode>.CreateListOfSize(1)
                                           .All()
                                           .With(e => e.SeasonNumber = 1)
                                           .Build()
                                           .ToList();

             _localEpisode = new LocalEpisode
                                {
                                    Path = @"C:\Test\Unsorted\30 Rock\30.rock.s01e01.avi".AsOsAgnostic(),
                                    Episodes = episodes,
                                    Series = _series
                                };
        }

        [Test]
        public void should_accept_if_media_info_is_null()
        {
            _localEpisode.MediaInfo = null;

            Subject.IsSatisfiedBy(_localEpisode, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_if_audio_stream_count_is_0()
        {
            _localEpisode.MediaInfo = Builder<MediaInfoModel>.CreateNew()
                .With(m => m.AudioStreams = [])
                .Build();

            Subject.IsSatisfiedBy(_localEpisode, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_if_audio_stream_count_is_0()
        {
            _localEpisode.MediaInfo = Builder<MediaInfoModel>.CreateNew()
                .With(m => m.AudioStreams =
                [
                    new MediaInfoAudioStreamModel { Language = "eng" },
                ])
                .Build();

            Subject.IsSatisfiedBy(_localEpisode, null).Accepted.Should().BeTrue();
        }
    }
}
