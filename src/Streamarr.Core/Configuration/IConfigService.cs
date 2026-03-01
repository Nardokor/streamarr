using System.Collections.Generic;
using Streamarr.Common.Http.Proxy;
using Streamarr.Core.Security;

namespace Streamarr.Core.Configuration
{
    public interface IConfigService
    {
        void SaveConfigDictionary(Dictionary<string, object> configValues);

        bool IsDefined(string key);

        // Media Management
        string RecycleBin { get; set; }
        int RecycleBinCleanupDays { get; set; }
        bool DeleteEmptyFolders { get; set; }
        bool SkipFreeSpaceCheckWhenImporting { get; set; }
        int MinimumFreeSpaceWhenImporting { get; set; }
        bool CopyUsingHardlinks { get; set; }

        // Permissions (Media Management)
        bool SetPermissionsLinux { get; set; }
        string ChmodFolder { get; set; }
        string ChownGroup { get; set; }

        // UI
        int FirstDayOfWeek { get; set; }
        string CalendarWeekColumnHeader { get; set; }

        string ShortDateFormat { get; set; }
        string LongDateFormat { get; set; }
        string TimeFormat { get; set; }
        string TimeZone { get; set; }
        bool ShowRelativeDates { get; set; }
        bool EnableColorImpairedMode { get; set; }
        int UILanguage { get; set; }

        // Archival
        string GlobalPriorityKeywords { get; set; }
        int DefaultRetentionDays { get; set; }

        // Download Client (yt-dlp)
        string YtDlpBinaryPath { get; set; }
        string YtDlpTempDownloadFolder { get; set; }
        string YtDlpCookieFilePath { get; set; }
        bool YtDlpEmbedMetadata { get; set; }
        bool YtDlpEmbedThumbnail { get; set; }
        string YtDlpPreferredFormat { get; set; }
        int YtDlpMaxConcurrentDownloads { get; set; }

        // Internal
        bool CleanupMetadataImages { get; set; }
        string PlexClientIdentifier { get; }

        // Forms Auth
        string RijndaelPassphrase { get; }
        string HmacPassphrase { get; }
        string RijndaelSalt { get; }
        string HmacSalt { get; }

        // Proxy
        bool ProxyEnabled { get; }
        ProxyType ProxyType { get; }
        string ProxyHostname { get; }
        int ProxyPort { get; }
        string ProxyUsername { get; }
        string ProxyPassword { get; }
        string ProxyBypassFilter { get; }
        bool ProxyBypassLocalAddresses { get; }

        // Backups
        string BackupFolder { get; }
        int BackupInterval { get; }
        int BackupRetention { get; }

        CertificateValidationType CertificateValidation { get; }
        string ApplicationUrl { get; }
    }
}
