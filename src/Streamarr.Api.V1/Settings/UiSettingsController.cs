using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Configuration;
using Streamarr.Core.Languages;
using Streamarr.Http;
using Streamarr.Http.REST.Attributes;

namespace Streamarr.Api.V1.Settings;

[V1ApiController("settings/ui")]
public class UiSettingsController : SettingsController<UiSettingsResource>
{
    private readonly IConfigFileProvider _configFileProvider;

    public UiSettingsController(IConfigFileProvider configFileProvider, IConfigService configService)
        : base(configService)
    {
        _configFileProvider = configFileProvider;
        SharedValidator.RuleFor(c => c.UiLanguage).Custom((value, context) =>
        {
            if (!Language.All.Any(o => o.Id == value))
            {
                context.AddFailure("Invalid UI Language value");
            }
        });

        SharedValidator.RuleFor(c => c.UiLanguage)
                       .GreaterThanOrEqualTo(1)
                       .WithMessage("The UI Language value cannot be less than 1");
    }

    [RestPutById]
    public override ActionResult<UiSettingsResource> SaveConfig([FromBody] UiSettingsResource resource)
    {
        var dictionary = resource.GetType()
                                 .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                 .ToDictionary(prop => prop.Name, prop => prop.GetValue(resource, null));

        _configFileProvider.SaveConfigDictionary(dictionary);
        _configService.SaveConfigDictionary(dictionary);

        return Accepted(resource.Id);
    }

    protected override UiSettingsResource ToResource(IConfigService model)
    {
        return UiSettingsResourceMapper.ToResource(_configFileProvider, model);
    }
}
