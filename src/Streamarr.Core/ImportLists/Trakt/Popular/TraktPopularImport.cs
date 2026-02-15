using NLog;
using Streamarr.Common.Http;
using Streamarr.Core.Configuration;
using Streamarr.Core.Localization;
using Streamarr.Core.Parser;

namespace Streamarr.Core.ImportLists.Trakt.Popular
{
    public class TraktPopularImport : TraktImportBase<TraktPopularSettings>
    {
        public TraktPopularImport(IImportListRepository netImportRepository,
                   IHttpClient httpClient,
                   IImportListStatusService netImportStatusService,
                   IConfigService configService,
                   IParsingService parsingService,
                   ILocalizationService localizationService,
                   Logger logger)
        : base(netImportRepository, httpClient, netImportStatusService, configService, parsingService, localizationService, logger)
        {
        }

        public override string Name => _localizationService.GetLocalizedString("ImportListsTraktSettingsPopularName");

        public override IParseImportListResponse GetParser()
        {
            return new TraktPopularParser(Settings);
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new TraktPopularRequestGenerator()
            {
                Settings = Settings,
                ClientId = ClientId
            };
        }
    }
}
