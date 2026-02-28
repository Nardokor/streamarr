using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Configuration;
using Streamarr.Core.MetadataSource.YouTube;
using Streamarr.Http;

namespace Streamarr.Api.V1.Settings;

[V1ApiController("settings/youtube")]
public class YouTubeSettingsController : SettingsController<YouTubeSettingsResource>
{
    private readonly IYouTubeApiClient _youTubeApiClient;

    public YouTubeSettingsController(IConfigService configService, IYouTubeApiClient youTubeApiClient)
        : base(configService)
    {
        _youTubeApiClient = youTubeApiClient;
    }

    protected override YouTubeSettingsResource ToResource(IConfigService model) =>
        YouTubeSettingsResourceMapper.ToResource(model);

    public override ActionResult<YouTubeSettingsResource> SaveConfig([FromBody] YouTubeSettingsResource resource)
    {
        var apiKey = resource.YouTubeApiKey ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                _youTubeApiClient.TestApiKey(apiKey);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"API key test failed: {ex.Message}" });
            }
        }

        return base.SaveConfig(resource);
    }

    [HttpPost("test")]
    public ActionResult TestConnection([FromBody] YouTubeSettingsResource resource)
    {
        try
        {
            _youTubeApiClient.TestApiKey(resource.YouTubeApiKey ?? string.Empty);

            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
