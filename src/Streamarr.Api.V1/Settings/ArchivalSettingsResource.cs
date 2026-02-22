using Streamarr.Core.Configuration;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Settings;

public class ArchivalSettingsResource : RestResource
{
    public string GlobalPriorityKeywords { get; set; } = string.Empty;
    public int DefaultRetentionDays { get; set; }
}

public static class ArchivalSettingsResourceMapper
{
    public static ArchivalSettingsResource ToResource(IConfigService config) =>
        new ArchivalSettingsResource
        {
            GlobalPriorityKeywords = config.GlobalPriorityKeywords,
            DefaultRetentionDays = config.DefaultRetentionDays,
        };
}
