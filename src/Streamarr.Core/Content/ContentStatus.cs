namespace Streamarr.Core.Content
{
    public enum ContentStatus
    {
        Unknown = 0,
        Missing = 1,
        Downloading = 2,
        Downloaded = 3,
        Deleted = 4,
        Recording = 5,   // Actively capturing a live stream
        Expired = 6,
        Modified = 7,
        Unwanted = 8,    // Filtered out / not wanted
        Processing = 9,  // Post-download processing in progress
        Available = 10,  // File deleted by retention; content still on platform (re-downloadable)
        Queued = 11      // Download command is queued, waiting for a free download thread
    }
}
