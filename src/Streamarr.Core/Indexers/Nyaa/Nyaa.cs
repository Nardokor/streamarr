using NLog;
using Streamarr.Common.Http;
using Streamarr.Core.Configuration;
using Streamarr.Core.Localization;
using Streamarr.Core.Parser;

namespace Streamarr.Core.Indexers.Nyaa
{
    public class Nyaa : HttpIndexerBase<NyaaSettings>
    {
        public override string Name => "Nyaa";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public Nyaa(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, Logger logger, ILocalizationService localizationService)
            : base(httpClient, indexerStatusService, configService, parsingService, logger, localizationService)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NyaaRequestGenerator() { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentRssParser() { UseGuidInfoUrl = true, SizeElementName = "size", InfoHashElementName = "infoHash", PeersElementName = "leechers", CalculatePeersAsSum = true, SeedsElementName = "seeders" };
        }
    }
}
