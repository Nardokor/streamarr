using Streamarr.Core.Download;
using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.MediaFiles.EpisodeImport
{
    public interface IImportDecisionEngineSpecification
    {
        ImportSpecDecision IsSatisfiedBy(LocalEpisode localEpisode, DownloadClientItem downloadClientItem);
    }
}
