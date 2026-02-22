using System;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Creators.Commands
{
    public class CheckLiveStreamsCommandExecutor : IExecute<CheckLiveStreamsCommand>
    {
        private readonly ICreatorService _creatorService;
        private readonly IChannelService _channelService;
        private readonly ILivestreamStatusService _livestreamStatusService;
        private readonly Logger _logger;

        public CheckLiveStreamsCommandExecutor(
            ICreatorService creatorService,
            IChannelService channelService,
            ILivestreamStatusService livestreamStatusService,
            Logger logger)
        {
            _creatorService = creatorService;
            _channelService = channelService;
            _livestreamStatusService = livestreamStatusService;
            _logger = logger;
        }

        public void Execute(CheckLiveStreamsCommand message)
        {
            var creators = _creatorService.GetMonitoredCreators();

            foreach (var creator in creators)
            {
                var channels = _channelService.GetByCreatorId(creator.Id);

                foreach (var channel in channels)
                {
                    if (!channel.Monitored || channel.Platform != PlatformType.YouTube)
                    {
                        continue;
                    }

                    try
                    {
                        _livestreamStatusService.RefreshLivestreamStatuses(channel);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to check live streams for channel '{0}'", channel.Title);
                    }
                }
            }
        }
    }
}
