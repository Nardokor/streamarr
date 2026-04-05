using Streamarr.Core.Messaging.Events;
using Streamarr.Core.ThingiProvider.Events;

namespace Streamarr.Core.MetadataSource
{
    public class MetadataSourceCookieCleanupService : IHandle<ProviderDeletedEvent<IMetadataSource>>
    {
        private readonly ICookieFileService _cookieFileService;

        public MetadataSourceCookieCleanupService(ICookieFileService cookieFileService)
        {
            _cookieFileService = cookieFileService;
        }

        public void Handle(ProviderDeletedEvent<IMetadataSource> message)
        {
            _cookieFileService.Delete(message.ProviderId);
        }
    }
}
