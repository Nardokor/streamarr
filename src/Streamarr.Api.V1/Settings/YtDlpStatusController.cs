using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Download.YtDlp;
using Streamarr.Http;

namespace Streamarr.Api.V1.Settings;

[V1ApiController("ytdlp")]
public class YtDlpStatusController : Controller
{
    private readonly IYtDlpClient _ytDlpClient;

    public YtDlpStatusController(IYtDlpClient ytDlpClient)
    {
        _ytDlpClient = ytDlpClient;
    }

    [HttpGet("status")]
    [Produces("application/json")]
    public YtDlpStatusResource GetStatus()
    {
        string? version = null;

        try
        {
            version = _ytDlpClient.GetVersion();
        }
        catch
        {
            // binary not found or not executable — version stays null
        }

        return new YtDlpStatusResource { Version = version };
    }
}

public class YtDlpStatusResource
{
    public string? Version { get; set; }
}
