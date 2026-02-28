using Streamarr.Core.Channels;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Channels;

public class ChannelResource : RestResource
{
    public int CreatorId { get; set; }
    public PlatformType Platform { get; set; }
    public string PlatformId { get; set; } = string.Empty;
    public string PlatformUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public bool Monitored { get; set; }
    public ChannelStatusType Status { get; set; }
    public DateTime? LastInfoSync { get; set; }

    // Wanted — content types
    public bool DownloadVideos { get; set; } = true;
    public bool DownloadShorts { get; set; } = true;
    public bool DownloadVods { get; set; } = true;
    public bool DownloadLive { get; set; }

    // Wanted — word filters
    public string WatchedWords { get; set; } = string.Empty;
    public string IgnoredWords { get; set; } = string.Empty;
    public bool WatchedDefeatsIgnored { get; set; } = true;

    // Download mode
    public bool AutoDownload { get; set; } = true;

    // Retention
    public int? RetentionDays { get; set; }
    public bool RetentionVideos { get; set; }
    public bool RetentionShorts { get; set; }
    public bool RetentionVods { get; set; }
    public bool RetentionLive { get; set; }
    public string RetentionExceptionWords { get; set; } = string.Empty;
}

public static class ChannelResourceMapper
{
    public static ChannelResource ToResource(this Channel model)
    {
        return new ChannelResource
        {
            Id = model.Id,
            CreatorId = model.CreatorId,
            Platform = model.Platform,
            PlatformId = model.PlatformId,
            PlatformUrl = model.PlatformUrl,
            Title = model.Title,
            Description = model.Description,
            ThumbnailUrl = model.ThumbnailUrl,
            Monitored = model.Monitored,
            Status = model.Status,
            LastInfoSync = model.LastInfoSync,
            DownloadVideos = model.DownloadVideos,
            DownloadShorts = model.DownloadShorts,
            DownloadVods = model.DownloadVods,
            DownloadLive = model.DownloadLive,
            WatchedWords = model.WatchedWords,
            IgnoredWords = model.IgnoredWords,
            WatchedDefeatsIgnored = model.WatchedDefeatsIgnored,
            AutoDownload = model.AutoDownload,
            RetentionDays = model.RetentionDays,
            RetentionVideos = model.RetentionVideos,
            RetentionShorts = model.RetentionShorts,
            RetentionVods = model.RetentionVods,
            RetentionLive = model.RetentionLive,
            RetentionExceptionWords = model.RetentionExceptionWords
        };
    }

    public static Channel ToModel(this ChannelResource resource)
    {
        return new Channel
        {
            Id = resource.Id,
            CreatorId = resource.CreatorId,
            Platform = resource.Platform,
            PlatformId = resource.PlatformId,
            PlatformUrl = resource.PlatformUrl,
            Title = resource.Title,
            Description = resource.Description,
            ThumbnailUrl = resource.ThumbnailUrl,
            Monitored = resource.Monitored,
            Status = resource.Status,
            DownloadVideos = resource.DownloadVideos,
            DownloadShorts = resource.DownloadShorts,
            DownloadVods = resource.DownloadVods,
            DownloadLive = resource.DownloadLive,
            WatchedWords = resource.WatchedWords,
            IgnoredWords = resource.IgnoredWords,
            WatchedDefeatsIgnored = resource.WatchedDefeatsIgnored,
            AutoDownload = resource.AutoDownload,
            RetentionDays = resource.RetentionDays,
            RetentionVideos = resource.RetentionVideos,
            RetentionShorts = resource.RetentionShorts,
            RetentionVods = resource.RetentionVods,
            RetentionLive = resource.RetentionLive,
            RetentionExceptionWords = resource.RetentionExceptionWords
        };
    }

    public static List<ChannelResource> ToResource(this IEnumerable<Channel> models)
    {
        return models.Select(ToResource).ToList();
    }
}
