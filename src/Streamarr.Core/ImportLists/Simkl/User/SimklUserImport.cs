using NLog;
using Streamarr.Common.Http;
using Streamarr.Core.Configuration;
using Streamarr.Core.Localization;
using Streamarr.Core.Parser;

namespace Streamarr.Core.ImportLists.Simkl.User
{
    public class SimklUserImport : SimklImportBase<SimklUserSettings>
    {
        public SimklUserImport(IImportListRepository netImportRepository,
                               IHttpClient httpClient,
                               IImportListStatusService netImportStatusService,
                               IConfigService configService,
                               IParsingService parsingService,
                               ILocalizationService localizationService,
                               Logger logger)
        : base(netImportRepository, httpClient, netImportStatusService, configService, parsingService, localizationService, logger)
        {
        }

        public override string Name => _localizationService.GetLocalizedString("ImportListsSimklSettingsName");

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new SimklUserRequestGenerator()
            {
                Settings = Settings,
                ClientId = ClientId
            };
        }
    }
}
