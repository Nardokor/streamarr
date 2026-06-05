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

        // True when a live attempt exited but its stderr showed network/fragment errors. yt-dlp
        // can exit 0 and merge a *truncated* file when a live connection drops — from its point of
        // view the stream "ended". This flag lets the supervisor distinguish a clean capture from
        // an interrupted one even when the exit code is 0, so it never accepts a truncated file as
        // a finished recording.
        public bool WasInterrupted { get; set; }
    }
}
