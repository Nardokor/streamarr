using System;
using System.Linq;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Lifecycle;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.MetadataSource.Twitch;
using Streamarr.Core.MetadataSource.YouTube;
using Streamarr.Core.RootFolders;

namespace Streamarr.Core.MetadataSource
{
    public class MetadataSourceSeeder : IHandle<ApplicationStartedEvent>
    {
        private readonly IMetadataSourceFactory _factory;
        private readonly IRootFolderService _rootFolderService;
        private readonly Logger _logger;

        public MetadataSourceSeeder(IMetadataSourceFactory factory, IRootFolderService rootFolderService, Logger logger)
        {
            _factory = factory;
            _rootFolderService = rootFolderService;
            _logger = logger;
        }

        public void Handle(ApplicationStartedEvent message)
        {
            SeedRootFolder();
            SeedYouTube();
            SeedTwitch();
        }

        private void SeedRootFolder()
        {
            var path = Environment.GetEnvironmentVariable("STREAMARR_ROOT_FOLDER");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (_rootFolderService.All().Any())
            {
                return;
            }

            try
            {
                _rootFolderService.Add(new RootFolder { Path = path });
                _logger.Info("Added root folder '{0}' from STREAMARR_ROOT_FOLDER", path);
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to seed root folder '{0}' from STREAMARR_ROOT_FOLDER: {1}", path, ex.Message);
            }
        }

        private void SeedYouTube()
        {
            var apiKey = Environment.GetEnvironmentVariable("STREAMARR_YOUTUBE_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return;
            }

            var existing = _factory.All().FirstOrDefault(d => d.Platform == PlatformType.YouTube);

            if (existing != null)
            {
                if (existing.Settings is YouTubeSettings settings && string.IsNullOrWhiteSpace(settings.ApiKey))
                {
                    settings.ApiKey = apiKey;
                    _factory.Update(existing);
                    _logger.Info("Seeded YouTube API key from STREAMARR_YOUTUBE_API_KEY");
                }
            }
            else
            {
                _factory.Create(new MetadataSourceDefinition
                {
                    Name = "YouTube",
                    Implementation = "YouTube",
                    ConfigContract = "YouTubeSettings",
                    Platform = PlatformType.YouTube,
                    Enable = true,
                    Settings = new YouTubeSettings { ApiKey = apiKey }
                });
                _logger.Info("Created YouTube metadata source from STREAMARR_YOUTUBE_API_KEY");
            }
        }

        private void SeedTwitch()
        {
            var clientId = Environment.GetEnvironmentVariable("STREAMARR_TWITCH_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("STREAMARR_TWITCH_CLIENT_SECRET");

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                return;
            }

            var existing = _factory.All().FirstOrDefault(d => d.Platform == PlatformType.Twitch);

            if (existing != null)
            {
                if (existing.Settings is TwitchSettings settings && string.IsNullOrWhiteSpace(settings.ClientId))
                {
                    settings.ClientId = clientId;
                    settings.ClientSecret = clientSecret;
                    _factory.Update(existing);
                    _logger.Info("Seeded Twitch credentials from STREAMARR_TWITCH_CLIENT_ID/SECRET");
                }
            }
            else
            {
                _factory.Create(new MetadataSourceDefinition
                {
                    Name = "Twitch",
                    Implementation = "Twitch",
                    ConfigContract = "TwitchSettings",
                    Platform = PlatformType.Twitch,
                    Enable = true,
                    Settings = new TwitchSettings { ClientId = clientId, ClientSecret = clientSecret }
                });
                _logger.Info("Created Twitch metadata source from STREAMARR_TWITCH_CLIENT_ID/SECRET");
            }
        }
    }
}
