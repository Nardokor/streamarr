using Streamarr.Core.Configuration;
using Streamarr.Http;

namespace Streamarr.Api.V1.Settings;

[V1ApiController("settings/youtube")]
public class YouTubeSettingsController : SettingsController<YouTubeSettingsResource>
{
    public YouTubeSettingsController(IConfigService configService)
        : base(configService)
    {
    }

    protected override YouTubeSettingsResource ToResource(IConfigService model) =>
        YouTubeSettingsResourceMapper.ToResource(model);
}
