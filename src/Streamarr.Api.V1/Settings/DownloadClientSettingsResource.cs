using Streamarr.Core.Configuration;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Settings;

public class DownloadClientSettingsResource : RestResource
{
    public string YtDlpBinaryPath { get; set; } = string.Empty;
    public string YtDlpTempDownloadFolder { get; set; } = string.Empty;
    public string YtDlpCookieFilePath { get; set; } = string.Empty;
    public bool YtDlpEmbedMetadata { get; set; }
    public bool YtDlpEmbedThumbnail { get; set; }
    public string YtDlpPreferredFormat { get; set; } = string.Empty;
    public int YtDlpMaxConcurrentDownloads { get; set; }
}

public static class DownloadClientSettingsMapper
{
    public static DownloadClientSettingsResource ToResource(IConfigService config)
    {
        return new DownloadClientSettingsResource
        {
            Id = 1,
            YtDlpBinaryPath = config.YtDlpBinaryPath,
            YtDlpTempDownloadFolder = config.YtDlpTempDownloadFolder,
            YtDlpCookieFilePath = config.YtDlpCookieFilePath,
            YtDlpEmbedMetadata = config.YtDlpEmbedMetadata,
            YtDlpEmbedThumbnail = config.YtDlpEmbedThumbnail,
            YtDlpPreferredFormat = config.YtDlpPreferredFormat,
            YtDlpMaxConcurrentDownloads = config.YtDlpMaxConcurrentDownloads,
        };
    }

    public static void SaveToConfig(this DownloadClientSettingsResource resource, IConfigService config)
    {
        config.YtDlpBinaryPath = resource.YtDlpBinaryPath;
        config.YtDlpTempDownloadFolder = resource.YtDlpTempDownloadFolder;
        config.YtDlpCookieFilePath = resource.YtDlpCookieFilePath;
        config.YtDlpEmbedMetadata = resource.YtDlpEmbedMetadata;
        config.YtDlpEmbedThumbnail = resource.YtDlpEmbedThumbnail;
        config.YtDlpPreferredFormat = resource.YtDlpPreferredFormat;
        config.YtDlpMaxConcurrentDownloads = resource.YtDlpMaxConcurrentDownloads;
    }
}
