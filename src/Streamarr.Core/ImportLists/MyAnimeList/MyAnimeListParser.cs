using System.Collections.Generic;
using NLog;
using Streamarr.Common.Extensions;
using Streamarr.Common.Instrumentation;
using Streamarr.Common.Serializer;
using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.ImportLists.MyAnimeList
{
    public class MyAnimeListParser(MyAnimeListSettings settings) : IParseImportListResponse
    {
        private static readonly Logger Logger = StreamarrLogger.GetLogger(typeof(MyAnimeListParser));
        private readonly MyAnimeListSettings _settings = settings;

        public IList<ImportListItemInfo> ParseResponse(ImportListResponse importListResponse)
        {
            var jsonResponse = Json.Deserialize<MyAnimeListResponse>(importListResponse.Content);
            var series = new List<ImportListItemInfo>();

            foreach (var show in jsonResponse.Animes)
            {
                if (show.ListStatus.Score < _settings.MinimumScore)
                {
                    Logger.Debug("Skipping {0} because score is below threshold", show.AnimeListInfo.Title);
                    continue;
                }

                series.AddIfNotNull(new ImportListItemInfo
                {
                    Title = show.AnimeListInfo.Title,
                    MalId = show.AnimeListInfo.Id
                });
            }

            return series;
        }
    }
}
