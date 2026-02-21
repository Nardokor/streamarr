using Streamarr.Core.Configuration;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Settings;

public class YouTubeSettingsResource : RestResource
{
    public string ApiKey { get; set; } = string.Empty;
}

public static class YouTubeSettingsResourceMapper
{
    public static YouTubeSettingsResource ToResource(IConfigService config) =>
        new YouTubeSettingsResource { ApiKey = config.YouTubeApiKey };
}
