using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Streamarr.Core.Channels;

namespace Streamarr.Core.Content
{
    public interface IContentFilterService
    {
        bool PassesFilter(string title, ContentType contentType, Channel channel, bool isMembers = false, bool isAccessible = true);
        void ReapplyFilterForChannel(Channel channel);
    }

    public class ContentFilterService : IContentFilterService
    {
        private readonly IContentService _contentService;
        private readonly Logger _logger;

        public ContentFilterService(IContentService contentService, Logger logger)
        {
            _contentService = contentService;
            _logger = logger;
        }

        public bool PassesFilter(string title, ContentType contentType, Channel channel, bool isMembers = false, bool isAccessible = true)
        {
            // Members gate: inaccessible members content is always unwanted;
            // accessible members content requires DownloadMembers to be enabled.
            if (isMembers && !isAccessible)
            {
                return false;
            }

            if (isMembers && !channel.DownloadMembers)
            {
                return false;
            }

            // 1. Content type gate
            var typeAllowed = contentType switch
            {
                ContentType.Video    => channel.DownloadVideos,
                ContentType.Short    => channel.DownloadShorts,
                ContentType.Vod      => channel.DownloadVods,
                ContentType.Live     => channel.DownloadLive,
                ContentType.Upcoming => channel.DownloadLive,
                _                    => true
            };

            if (!typeAllowed)
            {
                return false;
            }

            // 2. Word filters (only for type-allowed items)
            var lower = (title ?? string.Empty).ToLowerInvariant();
            var watchedTerms = ParseTerms(channel.WatchedWords);
            var ignoredTerms = ParseTerms(channel.IgnoredWords);

            var watchedEmpty      = watchedTerms.Length == 0;
            var passesWatched     = watchedEmpty || watchedTerms.Any(t => lower.Contains(t));
            var explicitlyWatched = !watchedEmpty && watchedTerms.Any(t => lower.Contains(t));
            var isIgnored         = ignoredTerms.Length > 0 && ignoredTerms.Any(t => lower.Contains(t));

            return passesWatched && (!isIgnored || (channel.WatchedDefeatsIgnored && explicitlyWatched));
        }

        public void ReapplyFilterForChannel(Channel channel)
        {
            var existing = _contentService.GetByChannelId(channel.Id);
            var toUpdate = new List<Content>();

            foreach (var content in existing)
            {
                if (content.Status != ContentStatus.Missing && content.Status != ContentStatus.Unwanted)
                {
                    continue;
                }

                var passes = PassesFilter(content.Title, content.ContentType, channel, content.IsMembers, content.IsAccessible);

                if (passes && content.Status == ContentStatus.Unwanted)
                {
                    content.Status = ContentStatus.Missing;
                    toUpdate.Add(content);
                }
                else if (!passes && content.Status == ContentStatus.Missing)
                {
                    content.Status = ContentStatus.Unwanted;
                    toUpdate.Add(content);
                }
            }

            foreach (var content in toUpdate)
            {
                _contentService.UpdateContent(content);
                _logger.Debug(
                    "Filter re-evaluation updated '{0}' → {1}",
                    content.Title,
                    content.Status);
            }
        }

        private static string[] ParseTerms(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<string>();
            }

            return input
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => t.Length > 0)
                .ToArray();
        }
    }
}
