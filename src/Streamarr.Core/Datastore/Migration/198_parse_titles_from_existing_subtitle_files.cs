using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Dapper;
using FluentMigrator;
using NLog;
using Streamarr.Common.Instrumentation;
using Streamarr.Core.Datastore.Migration.Framework;
using Streamarr.Core.MediaFiles.EpisodeImport.Aggregation.Aggregators;
using Streamarr.Core.Parser;
using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(198)]
    public class parse_title_from_existing_subtitle_files : StreamarrMigrationBase
    {
        private static readonly Logger Logger = StreamarrLogger.GetLogger(typeof(AggregateSubtitleInfo));

        protected override void MainDbUpgrade()
        {
            Alter.Table("SubtitleFiles").AddColumn("Title").AsString().Nullable();
            Alter.Table("SubtitleFiles").AddColumn("Copy").AsInt32().WithDefaultValue(0);
            Execute.WithConnection(UpdateTitles);
        }

        private void UpdateTitles(IDbConnection conn, IDbTransaction tran)
        {
            var updates = new List<object>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"SubtitleFiles\".\"Id\", \"SubtitleFiles\".\"RelativePath\", \"EpisodeFiles\".\"RelativePath\", \"EpisodeFiles\".\"OriginalFilePath\" FROM \"SubtitleFiles\" JOIN \"EpisodeFiles\" ON \"SubtitleFiles\".\"EpisodeFileId\" = \"EpisodeFiles\".\"Id\"";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var relativePath = reader.GetString(1);
                    var episodeFileRelativePath = reader.GetString(2);
                    var episodeFileOriginalFilePath = reader[3] as string;

                    var subtitleTitleInfo = CleanSubtitleTitleInfo(episodeFileRelativePath, episodeFileOriginalFilePath, relativePath);

                    updates.Add(new
                    {
                        Id = id,
                        Title = subtitleTitleInfo.Title,
                        Language = subtitleTitleInfo.Language,
                        LanguageTags = subtitleTitleInfo.LanguageTags,
                        Copy = subtitleTitleInfo.Copy
                    });
                }
            }

            var updateSubtitleFilesSql = "UPDATE \"SubtitleFiles\" SET \"Title\" = @Title, \"Copy\" = @Copy, \"Language\" = @Language, \"LanguageTags\" = @LanguageTags, \"LastUpdated\" = CURRENT_TIMESTAMP WHERE \"Id\" = @Id";
            conn.Execute(updateSubtitleFilesSql, updates, transaction: tran);
        }

        private static SubtitleTitleInfo CleanSubtitleTitleInfo(string relativePath, string originalFilePath, string path)
        {
            var subtitleTitleInfo = LanguageParser.ParseSubtitleLanguageInformation(path);

            var episodeFileTitle = Path.GetFileNameWithoutExtension(relativePath);
            var originalEpisodeFileTitle = Path.GetFileNameWithoutExtension(originalFilePath) ?? string.Empty;

            if (subtitleTitleInfo.TitleFirst && (episodeFileTitle.Contains(subtitleTitleInfo.RawTitle, StringComparison.OrdinalIgnoreCase) || originalEpisodeFileTitle.Contains(subtitleTitleInfo.RawTitle, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Debug("Subtitle title '{0}' is in episode file title '{1}'. Removing from subtitle title.", subtitleTitleInfo.RawTitle, episodeFileTitle);

                subtitleTitleInfo = LanguageParser.ParseBasicSubtitle(path);
            }

            var cleanedTags = subtitleTitleInfo.LanguageTags.Where(t => !episodeFileTitle.Contains(t, StringComparison.OrdinalIgnoreCase)).ToList();

            if (cleanedTags.Count != subtitleTitleInfo.LanguageTags.Count)
            {
                Logger.Debug("Removed language tags '{0}' from subtitle title '{1}'.", string.Join(", ", subtitleTitleInfo.LanguageTags.Except(cleanedTags)), subtitleTitleInfo.RawTitle);
                subtitleTitleInfo.LanguageTags = cleanedTags;
            }

            return subtitleTitleInfo;
        }
    }
}
