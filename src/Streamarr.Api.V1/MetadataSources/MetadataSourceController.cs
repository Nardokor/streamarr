using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Streamarr.Api.V1.Provider;
using Streamarr.Core.MetadataSource;
using Streamarr.Http;
using Streamarr.SignalR;

namespace Streamarr.Api.V1.MetadataSources
{
    [V1ApiController]
    public class MetadataSourceController : ProviderControllerBase<MetadataSourceResource, MetadataSourceBulkResource, IMetadataSource, MetadataSourceDefinition>
    {
        private readonly MetadataSourceFactory _metadataSourceFactory;
        private readonly ICookieFileService _cookieFileService;

        public MetadataSourceController(MetadataSourceFactory metadataSourceFactory,
                                        ICookieFileService cookieFileService,
                                        IBroadcastSignalRMessage signalRBroadcaster)
            : base(signalRBroadcaster,
                   metadataSourceFactory,
                   "metadatasource",
                   new MetadataSourceResourceMapper(),
                   new MetadataSourceBulkResourceMapper())
        {
            _metadataSourceFactory = metadataSourceFactory;
            _cookieFileService = cookieFileService;
        }

        [HttpGet("{id:int}/cookies")]
        public CookieStatusResource GetCookieStatus(int id)
        {
            return new CookieStatusResource
            {
                HasCookies = _cookieFileService.Exists(id)
            };
        }

        [HttpPost("{id:int}/cookies")]
        [DisableRequestSizeLimit]
        public async Task<CookieStatusResource> UploadCookies(int id, IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var path = _cookieFileService.Save(id, bytes);

            // Persist the path into the source's settings so it's available at runtime.
            var definition = _metadataSourceFactory.Get(id);
            var settings = (MetadataSourceSettingsBase)definition.Settings;
            settings.CookiesFilePath = path;
            _metadataSourceFactory.Update(definition);

            return new CookieStatusResource { HasCookies = true };
        }

        [HttpDelete("{id:int}/cookies")]
        public CookieStatusResource DeleteCookies(int id)
        {
            _cookieFileService.Delete(id);

            var definition = _metadataSourceFactory.Get(id);
            var settings = (MetadataSourceSettingsBase)definition.Settings;
            settings.CookiesFilePath = string.Empty;
            _metadataSourceFactory.Update(definition);

            return new CookieStatusResource { HasCookies = false };
        }
    }
}
