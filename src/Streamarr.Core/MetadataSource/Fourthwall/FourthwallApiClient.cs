using System;
using System.IO;
using System.Net;
using System.Net.Http;
using NLog;

namespace Streamarr.Core.MetadataSource.Fourthwall
{
    public interface IFourthwallApiClient
    {
        string FetchHtml(string cookiesFilePath, string url);
    }

    public class FourthwallApiClient : IFourthwallApiClient
    {
        private readonly Logger _logger;

        public FourthwallApiClient(Logger logger)
        {
            _logger = logger;
        }

        public string FetchHtml(string cookiesFilePath, string url)
        {
            var cookieContainer = LoadCookies(cookiesFilePath);
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                CheckCertificateRevocationList = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

            using var response = client.GetAsync(url).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Fourthwall request failed ({(int)response.StatusCode}) for {url}");
            }

            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        private CookieContainer LoadCookies(string cookiesFilePath)
        {
            var container = new CookieContainer();

            if (string.IsNullOrWhiteSpace(cookiesFilePath) || !File.Exists(cookiesFilePath))
            {
                _logger.Warn("Fourthwall cookies file not found: {0}", cookiesFilePath);
                return container;
            }

            foreach (var line in File.ReadAllLines(cookiesFilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                var parts = line.Split('\t');
                if (parts.Length < 7)
                {
                    continue;
                }

                var domain = parts[0].TrimStart('.');
                var path = parts[2];
                var secure = parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                var name = parts[5];
                var value = parts[6];

                try
                {
                    var scheme = secure ? "https" : "http";
                    container.Add(
                        new Uri($"{scheme}://{domain}"),
                        new Cookie(name, value, path, domain));
                }
                catch (Exception ex)
                {
                    _logger.Debug("Skipping malformed cookie entry for domain '{0}': {1}", domain, ex.Message);
                }
            }

            return container;
        }
    }
}
