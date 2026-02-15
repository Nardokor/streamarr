using System.Linq;
using Streamarr.Core.Download;
using Streamarr.Core.Parser;
using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.MediaFiles.EpisodeImport.Aggregation.Aggregators.Augmenters.Language
{
    public class AugmentLanguageFromFileName : IAugmentLanguage
    {
        public int Order => 1;
        public string Name => "FileName";

        public AugmentLanguageResult AugmentLanguage(LocalEpisode localEpisode, DownloadClientItem downloadClientItem)
        {
            var languages = localEpisode.FileEpisodeInfo?.Languages;

            if (languages == null)
            {
                return null;
            }

            foreach (var episode in localEpisode.Episodes)
            {
                var episodeTitleLanguage = LanguageParser.ParseLanguages(episode.Title);

                languages = languages.Except(episodeTitleLanguage).ToList();
            }

            return new AugmentLanguageResult(languages, Confidence.Filename);
        }
    }
}
