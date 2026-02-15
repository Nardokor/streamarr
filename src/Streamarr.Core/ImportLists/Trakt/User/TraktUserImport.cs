using NLog;
using Streamarr.Common.Http;
using Streamarr.Core.Configuration;
using Streamarr.Core.Localization;
using Streamarr.Core.Parser;

namespace Streamarr.Core.ImportLists.Trakt.User
{
    public class TraktUserImport : TraktImportBase<TraktUserSettings>
    {
        public TraktUserImport(IImportListRepository netImportRepository,
                               IHttpClient httpClient,
                               IImportListStatusService netImportStatusService,
                               IConfigService configService,
                               IParsingService parsingService,
                               ILocalizationService localizationService,
                               Logger logger)
        : base(netImportRepository, httpClient, netImportStatusService, configService, parsingService, localizationService, logger)
        {
        }

        public override string Name => _localizationService.GetLocalizedString("ImportListsTraktSettingsUserListName");

        public override IParseImportListResponse GetParser()
        {
            return new TraktUserParser(Settings);
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new TraktUserRequestGenerator(Settings, ClientId);
        }
    }
}
