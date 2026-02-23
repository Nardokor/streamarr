using System.Collections.Generic;
using System.Linq;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Download;
using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Creators.Commands
{
    public class DownloadMissingContentCommandExecutor : IExecute<DownloadMissingContentCommand>
    {
        private readonly ICreatorService _creatorService;
        private readonly IChannelService _channelService;
        private readonly IContentService _contentService;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public DownloadMissingContentCommandExecutor(ICreatorService creatorService,
                                                     IChannelService channelService,
                                                     IContentService contentService,
                                                     IManageCommandQueue commandQueueManager,
                                                     Logger logger)
        {
            _creatorService = creatorService;
            _channelService = channelService;
            _contentService = contentService;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        public void Execute(DownloadMissingContentCommand message)
        {
            var creators = message.CreatorId.HasValue
                ? new List<Creator> { _creatorService.GetCreator(message.CreatorId.Value) }
                : _creatorService.GetMonitoredCreators();

            foreach (var creator in creators)
            {
                QueueMissingDownloads(creator);
            }
        }

        private void QueueMissingDownloads(Creator creator)
        {
            _logger.Info("Queuing missing downloads for creator '{0}'", creator.Title);

            var channels = _channelService.GetByCreatorId(creator.Id);
            var downloadCommands = new List<DownloadContentCommand>();

            foreach (var channel in channels.Where(c => c.Monitored))
            {
                var missing = _contentService.GetMissingContent(channel.Id)
                    .Where(c => c.Monitored)
                    .Where(c => !channel.RecordLiveOnly || c.ContentType != ContentType.Livestream)
                    .Select(c => new DownloadContentCommand { ContentId = c.Id });

                downloadCommands.AddRange(missing);
            }

            if (downloadCommands.Any())
            {
                _logger.Info("Queuing {0} download(s) for creator '{1}'", downloadCommands.Count, creator.Title);
                _commandQueueManager.PushMany(downloadCommands);
            }
            else
            {
                _logger.Debug("No missing content to download for creator '{0}'", creator.Title);
            }
        }
    }
}
