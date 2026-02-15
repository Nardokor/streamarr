using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Housekeeping.Housekeepers;
using Streamarr.Core.Languages;
using Streamarr.Core.MediaFiles;
using Streamarr.Core.Qualities;
using Streamarr.Core.Test.Framework;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedEpisodeFilesFixture : DbTest<CleanupOrphanedEpisodeFiles, EpisodeFile>
    {
        [Test]
        public void should_delete_orphaned_episode_files()
        {
            var episodeFile = Builder<EpisodeFile>.CreateNew()
                .With(h => h.Languages = new List<Language> { Language.English })
                .With(h => h.Quality = new QualityModel())
                .BuildNew();

            Db.Insert(episodeFile);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_episode_files()
        {
            var episodeFiles = Builder<EpisodeFile>.CreateListOfSize(2)
                .All()
                .With(h => h.Languages = new List<Language> { Language.English })
                .With(h => h.Quality = new QualityModel())
                .BuildListOfNew();

            Db.InsertMany(episodeFiles);

            var episode = Builder<Episode>.CreateNew()
                .With(e => e.EpisodeFileId = episodeFiles.First().Id)
                .BuildNew();

            Db.Insert(episode);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            Db.All<Episode>().Should().Contain(e => e.EpisodeFileId == AllStoredModels.First().Id);
        }
    }
}
