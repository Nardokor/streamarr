using NLog;
using Streamarr.Core.Content;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Download
{
    public class DownloadQueueStatusHandler : IHandle<CommandQueuedEvent>
    {
        private readonly IContentService _contentService;
        private readonly Logger _logger;

        public DownloadQueueStatusHandler(IContentService contentService, Logger logger)
        {
            _contentService = contentService;
            _logger = logger;
        }

        public void Handle(CommandQueuedEvent message)
        {
            if (message.Command.Body is not DownloadContentCommand cmd)
            {
                return;
            }

            try
            {
                var content = _contentService.GetContent(cmd.ContentId);

                if (content.Status != ContentStatus.Missing && content.Status != ContentStatus.Unwanted)
                {
                    return;
                }

                content.Status = ContentStatus.Queued;
                _contentService.UpdateContent(content);

                _logger.Debug("Content {0} marked as Queued", cmd.ContentId);
            }
            catch
            {
                // Content may have been deleted; ignore
            }
        }
    }
}
