using Streamarr.Core.MediaFiles.EpisodeImport;
using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.MediaFiles.Commands
{
    public class DownloadedEpisodesScanCommand : Command
    {
        // Properties used by third-party apps, do not modify.
        public string Path { get; set; }
        public string DownloadClientId { get; set; }
        public ImportMode ImportMode { get; set; }
        public override bool RequiresDiskAccess => true;
        public override bool IsLongRunning => true;
    }
}
