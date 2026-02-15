using Streamarr.Core.Download.TrackedDownloads;

namespace Streamarr.Core.MediaFiles.EpisodeImport.Manual
{
    public class ManuallyImportedFile
    {
        public TrackedDownload TrackedDownload { get; set; }
        public ImportResult ImportResult { get; set; }
    }
}
