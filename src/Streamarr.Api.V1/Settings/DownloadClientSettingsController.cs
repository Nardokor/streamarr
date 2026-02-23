using Streamarr.Core.Configuration;
using Streamarr.Http;

namespace Streamarr.Api.V1.Settings;

[V1ApiController("settings/downloadclient")]
public class DownloadClientSettingsController : SettingsController<DownloadClientSettingsResource>
{
    public DownloadClientSettingsController(IConfigService configService)
        : base(configService)
    {
    }

    protected override DownloadClientSettingsResource ToResource(IConfigService model) =>
        DownloadClientSettingsMapper.ToResource(model);
}
