using System;
using NLog;
using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Download.YtDlp.Commands
{
    public class UpdateYtDlpCommandExecutor : IExecute<UpdateYtDlpCommand>
    {
        private readonly IYtDlpClient _ytDlpClient;
        private readonly Logger _logger;

        public UpdateYtDlpCommandExecutor(IYtDlpClient ytDlpClient)
        {
            _ytDlpClient = ytDlpClient;
            _logger = LogManager.GetCurrentClassLogger();
        }

        public void Execute(UpdateYtDlpCommand message)
        {
            _logger.Info("Checking for yt-dlp updates (nightly channel)");

            try
            {
                var result = _ytDlpClient.SelfUpdate();
                _logger.Info("yt-dlp update complete: {0}", result);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "yt-dlp self-update failed — the binary may not be writable by the current user. " +
                                 "In Docker, the container restart will pull the latest nightly automatically.");
            }
        }
    }
}
