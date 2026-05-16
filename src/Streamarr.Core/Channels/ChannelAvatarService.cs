using System;
using System.IO;
using NLog;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Common.Extensions;
using Streamarr.Common.Http;

namespace Streamarr.Core.Channels
{
    public interface IChannelAvatarService
    {
        // Returns the local filesystem path to the channel avatar, downloading it if needed.
        // Returns null if no thumbnail URL is set or download fails.
        string GetLocalAvatarPath(Channel channel);
    }

    public class ChannelAvatarService : IChannelAvatarService
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IHttpClient _httpClient;
        private readonly IChannelService _channelService;
        private readonly Logger _logger;

        public ChannelAvatarService(IAppFolderInfo appFolderInfo,
                                    IHttpClient httpClient,
                                    IChannelService channelService,
                                    Logger logger)
        {
            _appFolderInfo = appFolderInfo;
            _httpClient = httpClient;
            _channelService = channelService;
            _logger = logger;
        }

        public string GetLocalAvatarPath(Channel channel)
        {
            if (string.IsNullOrEmpty(channel.ThumbnailUrl))
            {
                return null;
            }

            var localPath = Path.Combine(_appFolderInfo.GetMediaCoverPath(), "Channel", channel.Id.ToString(), "avatar.jpg");

            if (channel.ThumbnailUrl.StartsWith("/MediaCover/", StringComparison.OrdinalIgnoreCase))
            {
                // Already cached — map the URL to the filesystem path.
                return Path.Combine(_appFolderInfo.GetAppDataPath(),
                    channel.ThumbnailUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            }

            if (File.Exists(localPath))
            {
                return localPath;
            }

            try
            {
                _logger.Debug("Downloading channel avatar for '{0}' from {1}", channel.Title, channel.ThumbnailUrl);
                _httpClient.DownloadFile(channel.ThumbnailUrl, localPath);

                var localUrl = $"/MediaCover/Channel/{channel.Id}/avatar.jpg";
                channel.ThumbnailUrl = localUrl;
                _channelService.UpdateChannel(channel);

                return localPath;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to download avatar for channel '{0}'", channel.Title);
                return null;
            }
        }
    }
}
