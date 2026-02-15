using System.Collections.Generic;
using Streamarr.Core.Extras.Metadata.Files;
using Streamarr.Core.MediaFiles;
using Streamarr.Core.ThingiProvider;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Extras.Metadata
{
    public interface IMetadata : IProvider
    {
        string GetFilenameAfterMove(Series series, EpisodeFile episodeFile, MetadataFile metadataFile);
        MetadataFile FindMetadataFile(Series series, string path);
        MetadataFileResult SeriesMetadata(Series series, SeriesMetadataReason reason);
        MetadataFileResult EpisodeMetadata(Series series, EpisodeFile episodeFile);
        List<ImageFileResult> SeriesImages(Series series);
        List<ImageFileResult> SeasonImages(Series series, Season season);
        List<ImageFileResult> EpisodeImages(Series series, EpisodeFile episodeFile);
    }

    public enum SeriesMetadataReason
    {
        Scan,
        EpisodeFolderCreated,
        EpisodesImported
    }
}
