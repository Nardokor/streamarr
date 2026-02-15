using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Configuration;
using Streamarr.Http.REST;
using Streamarr.Http.REST.Attributes;

namespace Streamarr.Api.V5.Settings
{
    public abstract class SettingsController<TResource> : RestController<TResource>
        where TResource : RestResource, new()
    {
        protected readonly IConfigService _configService;

        protected SettingsController(IConfigService configService)
        {
            _configService = configService;
        }

        protected override TResource GetResourceById(int id)
        {
            return GetConfig();
        }

        [HttpGet]
        [Produces("application/json")]
        public TResource GetConfig()
        {
            var resource = ToResource(_configService);
            resource.Id = 1;

            return resource;
        }

        [RestPutById]
        [Consumes("application/json")]
        public virtual ActionResult<TResource> SaveConfig([FromBody] TResource resource)
        {
            var dictionary = resource.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(resource, null));

            _configService.SaveConfigDictionary(dictionary);

            return Accepted(resource.Id);
        }

        protected abstract TResource ToResource(IConfigService model);
    }
}
