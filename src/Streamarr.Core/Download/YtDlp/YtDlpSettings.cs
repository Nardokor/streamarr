namespace Streamarr.Core.Download.YtDlp
{
    public class YtDlpSettings
    {
        public string BinaryPath { get; set; } = "yt-dlp";
        public string TempDownloadFolder { get; set; } = string.Empty;
        public bool EmbedMetadata { get; set; } = true;
        public bool EmbedThumbnail { get; set; } = true;
        public string PreferredFormat { get; set; } = "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best";
        public int MaxConcurrentDownloads { get; set; } = 1;
        public string DenoBinaryPath { get; set; } = "deno";
    }
}
