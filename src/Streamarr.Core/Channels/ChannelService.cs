using System.Collections.Generic;
using System.Linq;
using NLog;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.Notifications;

namespace Streamarr.Core.Channels
{
    public interface IChannelService
    {
        Channel GetChannel(int channelId);
        Channel FindByPlatformId(PlatformType platform, string platformId);
        List<Channel> GetByCreatorId(int creatorId);
        List<Channel> GetAllChannels();
        Channel AddChannel(Channel channel, string creatorTitle = "");
        Channel UpdateChannel(Channel channel);
        void DeleteChannel(int channelId);
        void DeleteByCreatorId(int creatorId);
    }

    public class ChannelService : IChannelService
    {
        private readonly IChannelRepository _repo;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public ChannelService(IChannelRepository repo,
                              IEventAggregator eventAggregator,
                              Logger logger)
        {
            _repo = repo;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public Channel GetChannel(int channelId)
        {
            return _repo.Get(channelId);
        }

        public Channel FindByPlatformId(PlatformType platform, string platformId)
        {
            return _repo.FindByPlatformId(platform, platformId);
        }

        public List<Channel> GetByCreatorId(int creatorId)
        {
            return _repo.GetByCreatorId(creatorId);
        }

        public List<Channel> GetAllChannels()
        {
            return _repo.All().ToList();
        }

        public Channel AddChannel(Channel channel, string creatorTitle = "")
        {
            _logger.Info("Adding channel '{0}' ({1}: {2})", channel.Title, channel.Platform, channel.PlatformId);
            var inserted = _repo.Insert(channel);

            _eventAggregator.PublishEvent(new ChannelAddedEvent
            {
                Message = new ChannelAddedMessage
                {
                    ChannelTitle = inserted.Title,
                    CreatorName = creatorTitle,
                    Platform = inserted.Platform,
                }
            });

            return inserted;
        }

        public Channel UpdateChannel(Channel channel)
        {
            _logger.Info("Updating channel '{0}'", channel.Title);
            return _repo.Update(channel);
        }

        public void DeleteChannel(int channelId)
        {
            _repo.Delete(channelId);
        }

        public void DeleteByCreatorId(int creatorId)
        {
            var channels = _repo.GetByCreatorId(creatorId);
            _repo.DeleteMany(channels);
        }
    }
}
