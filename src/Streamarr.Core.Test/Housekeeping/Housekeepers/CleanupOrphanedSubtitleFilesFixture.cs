using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Extras.Subtitles;
using Streamarr.Core.Housekeeping.Housekeepers;
using Streamarr.Core.Languages;
using Streamarr.Core.MediaFiles;
using Streamarr.Core.Qualities;
using Streamarr.Core.Test.Framework;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedSubtitleFilesFixture : DbTest<CleanupOrphanedSubtitleFiles, SubtitleFile>
    {
        [Test]
        public void should_delete_subtitle_files_that_dont_have_a_coresponding_series()
        {
            var episodeFile = Builder<EpisodeFile>.CreateNew()
                .With(h => h.Quality = new QualityModel())
                .With(h => h.Languages = new List<Language> { Language.English })
                .BuildNew();

            Db.Insert(episodeFile);

            var subtitleFile = Builder<SubtitleFile>.CreateNew()
                                                    .With(m => m.EpisodeFileId = episodeFile.Id)
                                                    .With(m => m.Language = Language.English)
                                                    .BuildNew();

            Db.Insert(subtitleFile);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_subtitle_files_that_have_a_coresponding_series()
        {
            var series = Builder<Series>.CreateNew()
                                        .BuildNew();

            var episodeFile = Builder<EpisodeFile>.CreateNew()
                .With(h => h.Quality = new QualityModel())
                .With(h => h.Languages = new List<Language> { Language.English })
                .BuildNew();

            Db.Insert(series);
            Db.Insert(episodeFile);

            var subtitleFile = Builder<SubtitleFile>.CreateNew()
                                                    .With(m => m.SeriesId = series.Id)
                                                    .With(m => m.EpisodeFileId = episodeFile.Id)
                                                    .With(m => m.Language = Language.English)
                                                    .BuildNew();

            Db.Insert(subtitleFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }

        [Test]
        public void should_delete_subtitle_files_that_dont_have_a_coresponding_episode_file()
        {
            var series = Builder<Series>.CreateNew()
                                        .BuildNew();

            Db.Insert(series);

            var subtitleFile = Builder<SubtitleFile>.CreateNew()
                                                    .With(m => m.SeriesId = series.Id)
                                                    .With(m => m.EpisodeFileId = 10)
                                                    .With(m => m.Language = Language.English)
                                                    .BuildNew();

            Db.Insert(subtitleFile);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_subtitle_files_that_have_a_coresponding_episode_file()
        {
            var series = Builder<Series>.CreateNew()
                                        .BuildNew();

            var episodeFile = Builder<EpisodeFile>.CreateNew()
                .With(h => h.Quality = new QualityModel())
                .With(h => h.Languages = new List<Language> { Language.English })
                .BuildNew();

            Db.Insert(series);
            Db.Insert(episodeFile);

            var subtitleFile = Builder<SubtitleFile>.CreateNew()
                                                    .With(m => m.SeriesId = series.Id)
                                                    .With(m => m.EpisodeFileId = episodeFile.Id)
                                                    .With(m => m.Language = Language.English)
                                                    .BuildNew();

            Db.Insert(subtitleFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }

        [Test]
        public void should_delete_subtitle_files_that_have_episodefileid_of_zero()
        {
            var series = Builder<Series>.CreateNew()
                                        .BuildNew();

            Db.Insert(series);

            var subtitleFile = Builder<SubtitleFile>.CreateNew()
                                                 .With(m => m.SeriesId = series.Id)
                                                 .With(m => m.EpisodeFileId = 0)
                                                 .With(m => m.Language = Language.English)
                                                 .BuildNew();

            Db.Insert(subtitleFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(0);
        }
    }
}
