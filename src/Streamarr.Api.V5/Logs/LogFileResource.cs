using Streamarr.Http.REST;

namespace Streamarr.Api.V5.Logs;

public class LogFileResource : RestResource
{
    public required string Filename { get; set; }
    public required DateTime LastWriteTime { get; set; }
    public required string ContentsUrl { get; set; }
    public required string DownloadUrl { get; set; }
}
