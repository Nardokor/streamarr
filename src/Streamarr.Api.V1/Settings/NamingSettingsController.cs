using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Organizer;
using Streamarr.Http;
using Streamarr.Http.REST.Attributes;

namespace Streamarr.Api.V1.Settings;

[V1ApiController("settings/naming")]
public class NamingSettingsController : Controller
{
    private readonly INamingConfigService _namingConfigService;

    public NamingSettingsController(INamingConfigService namingConfigService)
    {
        _namingConfigService = namingConfigService;
    }

    [HttpGet]
    [Produces("application/json")]
    public NamingSettingsResource GetConfig()
    {
        return _namingConfigService.GetConfig().ToResource();
    }

    [RestPutById]
    [Consumes("application/json")]
    public ActionResult<NamingSettingsResource> SaveConfig([FromBody] NamingSettingsResource resource)
    {
        var config = resource.ToModel();
        config.Id = 1;

        _namingConfigService.Save(config);

        return Accepted(config.Id);
    }
}
