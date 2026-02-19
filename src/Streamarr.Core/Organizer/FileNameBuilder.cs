using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Creators;
using ContentModel = Streamarr.Core.Content.Content;

namespace Streamarr.Core.Organizer
{
    public interface IFileNameBuilder
    {
        string BuildFileName(ContentModel content, Channel channel, Creator creator, ContentFile contentFile, NamingConfig namingConfig = null);
        string BuildFilePath(ContentModel content, Channel channel, Creator creator, ContentFile contentFile, string extension, NamingConfig namingConfig = null);
        string GetCreatorFolder(Creator creator, NamingConfig namingConfig = null);
    }

    public class FileNameBuilder : IFileNameBuilder
    {
        private static readonly Regex TokenRegex = new Regex(
            @"\{(?<token>[a-zA-Z][a-zA-Z0-9 ]*)\}",
            RegexOptions.Compiled);

        private static readonly char[] BadCharacters = { '\\', '/', '<', '>', '?', '*', '|', '"' };
        private static readonly char[] GoodCharacters = { '+', '+', '\0', '\0', '!', '-', '\0', '\0' };

        private readonly INamingConfigService _namingConfigService;
        private readonly Logger _logger;

        public FileNameBuilder(INamingConfigService namingConfigService, Logger logger)
        {
            _namingConfigService = namingConfigService;
            _logger = logger;
        }

        public string BuildFileName(ContentModel content, Channel channel, Creator creator, ContentFile contentFile, NamingConfig namingConfig = null)
        {
            namingConfig ??= _namingConfigService.GetConfig();

            if (!namingConfig.RenameContent)
            {
                if (!string.IsNullOrWhiteSpace(contentFile?.RelativePath))
                {
                    return Path.GetFileNameWithoutExtension(contentFile.RelativePath);
                }

                return content.Title;
            }

            var tokenHandlers = BuildTokenHandlers(content, channel, creator, contentFile);
            var result = ReplaceTokens(namingConfig.ContentFileFormat, tokenHandlers);

            result = CleanFileName(result, namingConfig);

            return result;
        }

        public string BuildFilePath(ContentModel content, Channel channel, Creator creator, ContentFile contentFile, string extension, NamingConfig namingConfig = null)
        {
            namingConfig ??= _namingConfigService.GetConfig();

            var fileName = BuildFileName(content, channel, creator, contentFile, namingConfig);
            var creatorFolder = GetCreatorFolder(creator, namingConfig);
            var creatorPath = creator.Path;

            return Path.Combine(creatorPath, fileName + extension);
        }

        public string GetCreatorFolder(Creator creator, NamingConfig namingConfig = null)
        {
            namingConfig ??= _namingConfigService.GetConfig();

            var tokenHandlers = new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Creator Title", () => creator.Title },
                { "Creator CleanTitle", () => creator.CleanTitle },
                { "Creator SortTitle", () => creator.SortTitle }
            };

            var result = ReplaceTokens(namingConfig.CreatorFolderFormat, tokenHandlers);

            return CleanFolderName(result, namingConfig);
        }

        private static Dictionary<string, Func<string>> BuildTokenHandlers(ContentModel content, Channel channel, Creator creator, ContentFile contentFile)
        {
            var handlers = new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // Creator tokens
                { "Creator Title", () => creator.Title },
                { "Creator CleanTitle", () => creator.CleanTitle },

                // Channel tokens
                { "Channel Title", () => channel?.Title ?? string.Empty },
                { "Channel Platform", () => channel?.Platform.ToString() ?? string.Empty },

                // Content tokens
                { "Content Title", () => content.Title },
                { "Content Id", () => content.PlatformContentId },
                { "Content Type", () => content.ContentType.ToString() },

                // Date tokens
                { "Published Date", () => content.AirDateUtc?.ToString("yyyy-MM-dd") ?? "Unknown" },
                { "Year", () => content.AirDateUtc?.ToString("yyyy") ?? "Unknown" },
                { "Month", () => content.AirDateUtc?.ToString("MM") ?? "Unknown" },
                { "Day", () => content.AirDateUtc?.ToString("dd") ?? "Unknown" },

                // Quality tokens
                { "Quality Title", () => contentFile?.Quality?.Quality?.Name ?? string.Empty },
                { "Quality Full", () => contentFile?.Quality?.ToString() ?? string.Empty }
            };

            return handlers;
        }

        private static string ReplaceTokens(string format, Dictionary<string, Func<string>> tokenHandlers)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return string.Empty;
            }

            return TokenRegex.Replace(format, match =>
            {
                var token = match.Groups["token"].Value;

                if (tokenHandlers.TryGetValue(token, out var handler))
                {
                    return handler() ?? string.Empty;
                }

                return match.Value;
            });
        }

        private static string CleanFileName(string name, NamingConfig namingConfig)
        {
            name = ReplaceColons(name, namingConfig.ColonReplacementFormat);

            if (namingConfig.ReplaceIllegalCharacters)
            {
                name = ReplaceBadCharacters(name);
            }

            return name.Trim();
        }

        private static string CleanFolderName(string name, NamingConfig namingConfig)
        {
            name = ReplaceColons(name, namingConfig.ColonReplacementFormat);

            if (namingConfig.ReplaceIllegalCharacters)
            {
                name = ReplaceBadCharacters(name);
            }

            return name.Trim();
        }

        private static string ReplaceColons(string input, ColonReplacementFormat format)
        {
            return format switch
            {
                ColonReplacementFormat.Delete => input.Replace(":", string.Empty),
                ColonReplacementFormat.Dash => input.Replace(":", "-"),
                ColonReplacementFormat.SpaceDash => input.Replace(":", " -"),
                ColonReplacementFormat.SpaceDashSpace => input.Replace(":", " - "),
                ColonReplacementFormat.Smart => input.Replace(": ", " - ").Replace(":", "-"),
                _ => input
            };
        }

        private static string ReplaceBadCharacters(string input)
        {
            for (var i = 0; i < BadCharacters.Length; i++)
            {
                if (GoodCharacters[i] == '\0')
                {
                    input = input.Replace(BadCharacters[i].ToString(), string.Empty);
                }
                else
                {
                    input = input.Replace(BadCharacters[i], GoodCharacters[i]);
                }
            }

            return input;
        }
    }
}
