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
    public string Category { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool Monitored { get; set; }
    public ChannelStatusType Status { get; set; }
    public DateTime? LastInfoSync { get; set; }
    public MembershipStatus MembershipStatus { get; set; }
    public DateTime? LastMembershipCheck { get; set; }

    // Wanted — content types
    public bool DownloadVideos { get; set; } = true;
    public bool DownloadShorts { get; set; } = true;
    public bool DownloadVods { get; set; } = true;
    public bool DownloadLive { get; set; }
    public bool DownloadMembers { get; set; }

    // Wanted — word filters
    public string WatchedWords { get; set; } = string.Empty;
    public string IgnoredWords { get; set; } = string.Empty;
    public bool WatchedDefeatsIgnored { get; set; } = true;

    // Download mode
    public bool AutoDownload { get; set; } = true;

    // Retention
    public int? RetentionDays { get; set; }
    public bool KeepVideos { get; set; } = true;
    public bool KeepShorts { get; set; } = true;
    public bool KeepVods { get; set; }
    public bool KeepMembers { get; set; }
    public string RetentionKeepWords { get; set; } = string.Empty;
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
            Category = model.Category,
            SortOrder = model.SortOrder,
            Monitored = model.Monitored,
            Status = model.Status,
            LastInfoSync = model.LastInfoSync,
            MembershipStatus = model.MembershipStatus,
            LastMembershipCheck = model.LastMembershipCheck,
            DownloadVideos = model.DownloadVideos,
            DownloadShorts = model.DownloadShorts,
            DownloadVods = model.DownloadVods,
            DownloadLive = model.DownloadLive,
            DownloadMembers = model.DownloadMembers,
            WatchedWords = model.WatchedWords,
            IgnoredWords = model.IgnoredWords,
            WatchedDefeatsIgnored = model.WatchedDefeatsIgnored,
            AutoDownload = model.AutoDownload,
            RetentionDays = model.RetentionDays,
            KeepVideos = model.KeepVideos,
            KeepShorts = model.KeepShorts,
            KeepVods = model.KeepVods,
            KeepMembers = model.KeepMembers,
            RetentionKeepWords = model.RetentionKeepWords
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
            Category = resource.Category,
            SortOrder = resource.SortOrder,
            Monitored = resource.Monitored,
            Status = resource.Status,
            LastInfoSync = resource.LastInfoSync,
            MembershipStatus = resource.MembershipStatus,
            LastMembershipCheck = resource.LastMembershipCheck,
            DownloadVideos = resource.DownloadVideos,
            DownloadShorts = resource.DownloadShorts,
            DownloadVods = resource.DownloadVods,
            DownloadLive = resource.DownloadLive,
            DownloadMembers = resource.DownloadMembers,
            WatchedWords = resource.WatchedWords,
            IgnoredWords = resource.IgnoredWords,
            WatchedDefeatsIgnored = resource.WatchedDefeatsIgnored,
            AutoDownload = resource.AutoDownload,
            RetentionDays = resource.RetentionDays,
            KeepVideos = resource.KeepVideos,
            KeepShorts = resource.KeepShorts,
            KeepVods = resource.KeepVods,
            KeepMembers = resource.KeepMembers,
            RetentionKeepWords = resource.RetentionKeepWords
        };
    }

    public static List<ChannelResource> ToResource(this IEnumerable<Channel> models)
    {
        return models.Select(ToResource).ToList();
    }
}
