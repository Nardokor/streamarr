using NLog;
using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.MetadataSource.YouTube
{
    public class RenewWebSubSubscriptionsCommandExecutor : IExecute<RenewWebSubSubscriptionsCommand>
    {
        private readonly IYoutubeWebSubService _webSubService;
        private readonly Logger _logger;

        public RenewWebSubSubscriptionsCommandExecutor(IYoutubeWebSubService webSubService)
        {
            _webSubService = webSubService;
            _logger = LogManager.GetCurrentClassLogger();
        }

        public void Execute(RenewWebSubSubscriptionsCommand message)
        {
            _webSubService.SubscribeAll();
        }
    }
}
