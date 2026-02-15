using Streamarr.Core.Download;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Qualities;

namespace Streamarr.Core.MediaFiles.EpisodeImport.Aggregation.Aggregators.Augmenters.Quality
{
    public class AugmentQualityFromFolder : IAugmentQuality
    {
        public int Order => 2;
        public string Name => "FolderName";

        public AugmentQualityResult AugmentQuality(LocalEpisode localEpisode, DownloadClientItem downloadClientItem)
        {
            var quality = localEpisode.FolderEpisodeInfo?.Quality;

            if (quality == null)
            {
                return null;
            }

            var sourceConfidence = quality.SourceDetectionSource == QualityDetectionSource.Name
                ? Confidence.Tag
                : Confidence.Fallback;

            var resolutionConfidence = quality.ResolutionDetectionSource == QualityDetectionSource.Name
                ? Confidence.Tag
                : Confidence.Fallback;

            var revisionConfidence = quality.RevisionDetectionSource == QualityDetectionSource.Name
                ? Confidence.Tag
                : Confidence.Fallback;

            return new AugmentQualityResult(quality.Quality.Source,
                                            sourceConfidence,
                                            quality.Quality.Resolution,
                                            resolutionConfidence,
                                            quality.Revision,
                                            revisionConfidence);
        }
    }
}
