namespace Streamarr.Core.Content
{
    public enum ContentType
    {
        Unknown = 0,
        Video = 1,
        Short = 2,
        Vod = 3,      // Archived/completed livestream
        Live = 4,     // Currently airing
        Upcoming = 5  // Scheduled, not yet started
    }
}
