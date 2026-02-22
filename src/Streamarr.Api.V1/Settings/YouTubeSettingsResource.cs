using Streamarr.Core.Configuration;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Settings;

public class YouTubeSettingsResource : RestResource
{
    public string YouTubeApiKey { get; set; } = string.Empty;
    public int YouTubeFullRefreshIntervalHours { get; set; }
    public int YouTubeLiveCheckIntervalMinutes { get; set; }
}

public static class YouTubeSettingsResourceMapper
{
    public static YouTubeSettingsResource ToResource(IConfigService config) =>
        new YouTubeSettingsResource
        {
            YouTubeApiKey = config.YouTubeApiKey,
            YouTubeFullRefreshIntervalHours = config.YouTubeFullRefreshIntervalHours,
            YouTubeLiveCheckIntervalMinutes = config.YouTubeLiveCheckIntervalMinutes,
        };
}
