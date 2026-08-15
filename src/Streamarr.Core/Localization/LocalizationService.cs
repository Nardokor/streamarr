using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using NLog;
using Streamarr.Common.Cache;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Core.Configuration;
using Streamarr.Core.Configuration.Events;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Localization
{
    public interface ILocalizationService
    {
        Dictionary<string, string> GetLocalizationDictionary();

        string GetLocalizedString(string phrase);
        string GetLocalizedString(string phrase, Dictionary<string, object> tokens);
        string GetLanguageIdentifier();
    }

    public class LocalizationService : ILocalizationService, IHandleAsync<ConfigSavedEvent>
    {
        private const string DefaultCulture = "en";
        private static readonly Regex TokenRegex = new Regex(@"(?:\{)(?<token>[a-z0-9]+)(?:\})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private readonly ICached<Dictionary<string, string>> _cache;

        private readonly IAppFolderInfo _appFolderInfo;
        private readonly Logger _logger;

        public LocalizationService(IConfigService configService,
                                   IAppFolderInfo appFolderInfo,
                                   ICacheManager cacheManager,
                                   Logger logger)
        {
            _appFolderInfo = appFolderInfo;
            _cache = cacheManager.GetCache<Dictionary<string, string>>(typeof(Dictionary<string, string>), "localization");
            _logger = logger;
        }

        public Dictionary<string, string> GetLocalizationDictionary()
        {
            return GetLocalizationDictionary(DefaultCulture);
        }

        public string GetLocalizedString(string phrase)
        {
            return GetLocalizedString(phrase, new Dictionary<string, object>());
        }

        public string GetLocalizedString(string phrase, Dictionary<string, object> tokens)
        {
            if (string.IsNullOrEmpty(phrase))
            {
                throw new ArgumentNullException(nameof(phrase));
            }

            var dictionary = GetLocalizationDictionary(DefaultCulture);

            if (dictionary.TryGetValue(phrase, out var value))
            {
                return ReplaceTokens(value, tokens);
            }

            return phrase;
        }

        public string GetLanguageIdentifier()
        {
            return DefaultCulture;
        }

        private string ReplaceTokens(string input, Dictionary<string, object> tokens)
        {
            tokens.TryAdd("appName", "Streamarr");

            return TokenRegex.Replace(input, (match) =>
            {
                var tokenName = match.Groups["token"].Value;

                tokens.TryGetValue(tokenName, out var token);

                return token?.ToString() ?? $"{{{tokenName}}}";
            });
        }

        private Dictionary<string, string> GetLocalizationDictionary(string language)
        {
            var startupFolder = _appFolderInfo.StartUpFolder;
            var prefix = Path.Combine(startupFolder, "Localization", "Core");

            return _cache.Get("localization", () => LoadDictionary(prefix));
        }

        private Dictionary<string, string> LoadDictionary(string prefix)
        {
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var filePath = Path.Combine(prefix, DefaultCulture + ".json");

            if (!File.Exists(filePath))
            {
                _logger.Error("Missing localization resource: {0}", filePath);
                return dictionary;
            }

            using var fs = File.OpenRead(filePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(fs);

            if (dict == null)
            {
                _logger.Error("Failed to deserialize localization resource: {0}", filePath);
                return dictionary;
            }

            foreach (var key in dict.Keys)
            {
                dictionary[key] = dict[key];
            }

            return dictionary;
        }

        public void HandleAsync(ConfigSavedEvent message)
        {
            _cache.Clear();
        }
    }
}
