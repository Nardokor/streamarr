using System;
using System.Text.RegularExpressions;

namespace Streamarr.Core.Download.YtDlp
{
    // Recognizes yt-dlp's intermediate/partial file naming so callers can distinguish a
    // finished download from a fragment, part, or temp file left behind mid-download.
    public static class YtDlpFileClassifier
    {
        private static readonly Regex FormatFragmentRegex = new Regex(@"\.f\d+\.", RegexOptions.Compiled);

        public static bool IsIntermediate(string fileName) =>
            fileName.Contains(".part", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("-Frag", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".temp", StringComparison.OrdinalIgnoreCase) ||
            FormatFragmentRegex.IsMatch(fileName);
    }
}
