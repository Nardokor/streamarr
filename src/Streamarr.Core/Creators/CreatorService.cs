using System.Collections.Generic;
using System.Linq;
using NLog;
using Streamarr.Common.Disk;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Creators.Events;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Creators
{
    public interface ICreatorService
    {
        Creator GetCreator(int creatorId);
        Creator FindByTitle(string cleanTitle);
        List<Creator> GetAllCreators();
        List<Creator> GetMonitoredCreators();
        Creator AddCreator(Creator creator);
        Creator UpdateCreator(Creator creator);
        void DeleteCreator(int creatorId);
        bool CreatorPathExists(string path);
    }

    public class CreatorService : ICreatorService
    {
        private readonly ICreatorRepository _repo;
        private readonly IChannelService _channelService;
        private readonly IContentService _contentService;
        private readonly IDiskProvider _diskProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public CreatorService(ICreatorRepository repo,
                              IChannelService channelService,
                              IContentService contentService,
                              IDiskProvider diskProvider,
                              IEventAggregator eventAggregator,
                              Logger logger)
        {
            _repo = repo;
            _channelService = channelService;
            _contentService = contentService;
            _diskProvider = diskProvider;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public Creator GetCreator(int creatorId)
        {
            return _repo.Get(creatorId);
        }

        public Creator FindByTitle(string cleanTitle)
        {
            return _repo.FindByTitle(cleanTitle);
        }

        public List<Creator> GetAllCreators()
        {
            return _repo.All().ToList();
        }

        public List<Creator> GetMonitoredCreators()
        {
            return _repo.All().Where(c => c.Monitored).ToList();
        }

        public Creator AddCreator(Creator creator)
        {
            _logger.Info("Adding creator '{0}'", creator.Title);

            creator.CleanTitle = creator.Title.CleanCreatorTitle();
            creator.SortTitle = creator.Title?.ToLowerInvariant() ?? string.Empty;

            _diskProvider.EnsureFolder(creator.Path);

            _repo.Insert(creator);
            _eventAggregator.PublishEvent(new CreatorAddedEvent(creator));

            return creator;
        }

        public Creator UpdateCreator(Creator creator)
        {
            _logger.Info("Updating creator '{0}'", creator.Title);

            creator.CleanTitle = creator.Title.CleanCreatorTitle();
            creator.SortTitle = creator.Title?.ToLowerInvariant() ?? string.Empty;

            _repo.Update(creator);
            _eventAggregator.PublishEvent(new CreatorUpdatedEvent(creator));

            return creator;
        }

        public void DeleteCreator(int creatorId)
        {
            var creator = _repo.Get(creatorId);

            _logger.Info("Deleting creator '{0}'", creator.Title);

            var channels = _channelService.GetByCreatorId(creatorId);
            foreach (var channel in channels)
            {
                _contentService.DeleteByChannelId(channel.Id);
            }

            _channelService.DeleteByCreatorId(creatorId);
            _repo.Delete(creatorId);
            _eventAggregator.PublishEvent(new CreatorDeletedEvent(creator));
        }

        public bool CreatorPathExists(string path)
        {
            return _repo.CreatorPathExists(path);
        }
    }
}
