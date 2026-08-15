using System.Linq;
using NLog;
using Streamarr.Common.Instrumentation.Extensions;
using Streamarr.Core.Content;
using Streamarr.Core.Lifecycle;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Download
{
    public class ResumeInterruptedDownloadsService : IHandle<ApplicationStartedEvent>
    {
        private readonly IContentService _contentService;
        private readonly IManageCommandQueue _commandQueue;
        private readonly Logger _logger;

        public ResumeInterruptedDownloadsService(IContentService contentService,
                                                 IManageCommandQueue commandQueue,
                                                 Logger logger)
        {
            _contentService = contentService;
            _commandQueue = commandQueue;
            _logger = logger;
        }

        public void Handle(ApplicationStartedEvent message)
        {
            var interrupted = _contentService.GetAllRecording()
                .Concat(_contentService.GetAllDownloading())
                .ToList();

            if (interrupted.Count == 0)
            {
                return;
            }

            _logger.ProgressInfo("Resuming {0} interrupted download(s) after restart", interrupted.Count);

            foreach (var content in interrupted)
            {
                _commandQueue.Push(new DownloadContentCommand
                {
                    ContentId = content.Id,
                    IsResume = true,
                });

                _logger.Debug("Re-queued download for '{0}' (content {1})", content.Title, content.Id);
            }
        }
    }
}
