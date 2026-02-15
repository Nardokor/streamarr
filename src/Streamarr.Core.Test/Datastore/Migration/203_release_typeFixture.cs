using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Common.Extensions;
using Streamarr.Common.Serializer;
using Streamarr.Core.Datastore.Migration;
using Streamarr.Core.MediaFiles.MediaInfo;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Qualities;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class release_typeFixture : MigrationTest<release_type>
    {
        [Test]
        public void should_convert_single_episode_without_folder()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("EpisodeFiles").Row(new
                {
                    SeriesId = 1,
                    SeasonNumber = 1,
                    RelativePath = "Season 01/S01E05.mkv",
                    Size = 125.Megabytes(),
                    DateAdded = DateTime.UtcNow.AddDays(-5),
                    OriginalFilePath = "Series.Title.S01E05.720p.HDTV.x265-Streamarr.mkv",
                    ReleaseGroup = "Streamarr",
                    Quality = new QualityModel(Quality.HDTV720p).ToJson(),
                    Languages = "[1]"
                });
            });

            var items = db.Query<EpisodeFile203>("SELECT * FROM \"EpisodeFiles\"");

            items.Should().HaveCount(1);

            items.First().ReleaseType.Should().Be((int)ReleaseType.SingleEpisode);
        }

        [Test]
        public void should_convert_single_episode_with_folder()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("EpisodeFiles").Row(new
                {
                    SeriesId = 1,
                    SeasonNumber = 1,
                    RelativePath = "Season 01/S01E05.mkv",
                    Size = 125.Megabytes(),
                    DateAdded = DateTime.UtcNow.AddDays(-5),
                    OriginalFilePath = "Series.Title.S01E05.720p.HDTV.x265-Streamarr/S01E05.mkv",
                    ReleaseGroup = "Streamarr",
                    Quality = new QualityModel(Quality.HDTV720p).ToJson(),
                    Languages = "[1]"
                });
            });

            var items = db.Query<EpisodeFile203>("SELECT * FROM \"EpisodeFiles\"");

            items.Should().HaveCount(1);

            items.First().ReleaseType.Should().Be((int)ReleaseType.SingleEpisode);
        }

        [Test]
        public void should_convert_multi_episode_without_folder()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("EpisodeFiles").Row(new
                {
                    SeriesId = 1,
                    SeasonNumber = 1,
                    RelativePath = "Season 01/S01E05.mkv",
                    Size = 125.Megabytes(),
                    DateAdded = DateTime.UtcNow.AddDays(-5),
                    OriginalFilePath = "Series.Title.S01E05E06.720p.HDTV.x265-Streamarr.mkv",
                    ReleaseGroup = "Streamarr",
                    Quality = new QualityModel(Quality.HDTV720p).ToJson(),
                    Languages = "[1]"
                });
            });

            var items = db.Query<EpisodeFile203>("SELECT * FROM \"EpisodeFiles\"");

            items.Should().HaveCount(1);

            items.First().ReleaseType.Should().Be((int)ReleaseType.MultiEpisode);
        }

        [Test]
        public void should_convert_multi_episode_with_folder()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("EpisodeFiles").Row(new
                {
                    SeriesId = 1,
                    SeasonNumber = 1,
                    RelativePath = "Season 01/S01E05.mkv",
                    Size = 125.Megabytes(),
                    DateAdded = DateTime.UtcNow.AddDays(-5),
                    OriginalFilePath = "Series.Title.S01E05E06.720p.HDTV.x265-Streamarr/S01E05E06.mkv",
                    ReleaseGroup = "Streamarr",
                    Quality = new QualityModel(Quality.HDTV720p).ToJson(),
                    Languages = "[1]"
                });
            });

            var items = db.Query<EpisodeFile203>("SELECT * FROM \"EpisodeFiles\"");

            items.Should().HaveCount(1);

            items.First().ReleaseType.Should().Be((int)ReleaseType.MultiEpisode);
        }

        [Test]
        public void should_convert_season_pack_with_folder()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("EpisodeFiles").Row(new
                {
                    SeriesId = 1,
                    SeasonNumber = 1,
                    RelativePath = "Season 01/S01E05.mkv",
                    Size = 125.Megabytes(),
                    DateAdded = DateTime.UtcNow.AddDays(-5),
                    OriginalFilePath = "Series.Title.S01.720p.HDTV.x265-Streamarr/S01E05.mkv",
                    ReleaseGroup = "Streamarr",
                    Quality = new QualityModel(Quality.HDTV720p).ToJson(),
                    Languages = "[1]"
                });
            });

            var items = db.Query<EpisodeFile203>("SELECT * FROM \"EpisodeFiles\"");

            items.Should().HaveCount(1);

            items.First().ReleaseType.Should().Be((int)ReleaseType.SeasonPack);
        }

        [Test]
        public void should_not_convert_episode_without_original_file_path()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("EpisodeFiles").Row(new
                {
                    SeriesId = 1,
                    SeasonNumber = 1,
                    RelativePath = "Season 01/S01E05.mkv",
                    Size = 125.Megabytes(),
                    DateAdded = DateTime.UtcNow.AddDays(-5),
                    ReleaseGroup = "Streamarr",
                    Quality = new QualityModel(Quality.HDTV720p).ToJson(),
                    Languages = "[1]"
                });
            });

            var items = db.Query<EpisodeFile203>("SELECT * FROM \"EpisodeFiles\"");

            items.Should().HaveCount(1);

            items.First().ReleaseType.Should().Be((int)ReleaseType.Unknown);
        }

        public class EpisodeFile203
        {
            public int Id { get; set; }
            public int SeriesId { get; set; }
            public int SeasonNumber { get; set; }
            public string RelativePath { get; set; }
            public long Size { get; set; }
            public DateTime DateAdded { get; set; }
            public string OriginalFilePath { get; set; }
            public string SceneName { get; set; }
            public string ReleaseGroup { get; set; }
            public QualityModel Quality { get; set; }
            public long IndexerFlags { get; set; }
            public MediaInfoModel MediaInfo { get; set; }
            public List<int> Languages { get; set; }
            public long ReleaseType { get; set; }
        }
    }
}
