using NLog;
using Streamarr.Common.Disk;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Common.Extensions;
using Streamarr.Core.Instrumentation.Commands;
using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Instrumentation
{
    public interface IDeleteLogFilesService
    {
    }

    public class DeleteLogFilesService : IDeleteLogFilesService, IExecute<DeleteLogFilesCommand>, IExecute<DeleteUpdateLogFilesCommand>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly Logger _logger;

        public DeleteLogFilesService(IDiskProvider diskProvider, IAppFolderInfo appFolderInfo, Logger logger)
        {
            _diskProvider = diskProvider;
            _appFolderInfo = appFolderInfo;
            _logger = logger;
        }

        public void Execute(DeleteLogFilesCommand message)
        {
            _logger.Debug("Deleting all files in: {0}", _appFolderInfo.GetLogFolder());
            _diskProvider.EmptyFolder(_appFolderInfo.GetLogFolder());
        }

        public void Execute(DeleteUpdateLogFilesCommand message)
        {
            _logger.Debug("Deleting all files in: {0}", _appFolderInfo.GetUpdateLogFolder());
            _diskProvider.EmptyFolder(_appFolderInfo.GetUpdateLogFolder());
        }
    }
}
