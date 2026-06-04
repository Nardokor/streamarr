namespace Streamarr.Core.Download.YtDlp
{
    public class YtDlpProgress
    {
        public string Status { get; set; } = string.Empty;
        public double? PercentComplete { get; set; }
        public string Speed { get; set; } = string.Empty;
        public string Eta { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public long? TotalBytes { get; set; }
        public long? DownloadedBytes { get; set; }
    }

    public class YtDlpDownloadResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int ExitCode { get; set; }

        // True when the output file came from a yt-dlp merge step (a complete, finalized file)
        // rather than a bare last fragment. The live-recording supervisor uses this to tell a
        // cleanly-ended stream (merged) apart from an interrupted attempt that only produced
        // partial fragments — the latter must be relaunched, not treated as finished.
        public bool IsMergedOutput { get; set; }
    }
}
