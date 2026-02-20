using System;
using System.IO;
using NLog;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Common.Extensions;
using Streamarr.Common.Http;

namespace Streamarr.Core.Creators
{
    public interface ICreatorAvatarService
    {
        void DownloadAvatar(Creator creator);
    }

    public class CreatorAvatarService : ICreatorAvatarService
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IHttpClient _httpClient;
        private readonly ICreatorService _creatorService;
        private readonly Logger _logger;

        public CreatorAvatarService(IAppFolderInfo appFolderInfo,
                                    IHttpClient httpClient,
                                    ICreatorService creatorService,
                                    Logger logger)
        {
            _appFolderInfo = appFolderInfo;
            _httpClient = httpClient;
            _creatorService = creatorService;
            _logger = logger;
        }

        public void DownloadAvatar(Creator creator)
        {
            if (string.IsNullOrEmpty(creator.ThumbnailUrl) ||
                creator.ThumbnailUrl.StartsWith("/MediaCover/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var localPath = Path.Combine(_appFolderInfo.GetMediaCoverPath(), "Creator", creator.Id.ToString(), "avatar.jpg");
            var localUrl = $"/MediaCover/Creator/{creator.Id}/avatar.jpg";

            try
            {
                _logger.Debug("Downloading creator avatar for '{0}' from {1}", creator.Title, creator.ThumbnailUrl);
                _httpClient.DownloadFile(creator.ThumbnailUrl, localPath);
                creator.ThumbnailUrl = localUrl;
                _creatorService.UpdateCreator(creator);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to download avatar for creator '{0}', keeping remote URL", creator.Title);
            }
        }
    }
}
