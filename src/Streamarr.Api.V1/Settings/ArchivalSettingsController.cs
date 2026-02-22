using Streamarr.Core.Configuration;
using Streamarr.Http;

namespace Streamarr.Api.V1.Settings;

[V1ApiController("settings/archival")]
public class ArchivalSettingsController : SettingsController<ArchivalSettingsResource>
{
    public ArchivalSettingsController(IConfigService configService)
        : base(configService)
    {
    }

    protected override ArchivalSettingsResource ToResource(IConfigService model) =>
        ArchivalSettingsResourceMapper.ToResource(model);
}
