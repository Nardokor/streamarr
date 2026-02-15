using System;
using System.IO;
using NLog;
using Streamarr.Common;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Common.Extensions;
using Streamarr.Common.Processes;
using IServiceProvider = Streamarr.Common.IServiceProvider;

namespace Streamarr.Update.UpdateEngine
{
    public interface IStartStreamarr
    {
        void Start(AppType appType, string installationFolder);
    }

    public class StartStreamarr : IStartStreamarr
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IProcessProvider _processProvider;
        private readonly IStartupContext _startupContext;
        private readonly Logger _logger;

        public StartStreamarr(IServiceProvider serviceProvider, IProcessProvider processProvider, IStartupContext startupContext, Logger logger)
        {
            _serviceProvider = serviceProvider;
            _processProvider = processProvider;
            _startupContext = startupContext;
            _logger = logger;
        }

        public void Start(AppType appType, string installationFolder)
        {
            _logger.Info("Starting Streamarr");
            if (appType == AppType.Service)
            {
                try
                {
                    StartService();
                }
                catch (InvalidOperationException e)
                {
                    _logger.Warn("Couldn't start Streamarr Service (Most likely due to permission issues). falling back to console.", e);
                    StartConsole(installationFolder);
                }
            }
            else if (appType == AppType.Console)
            {
                StartConsole(installationFolder);
            }
            else
            {
                StartWinform(installationFolder);
            }
        }

        private void StartService()
        {
            _logger.Info("Starting Streamarr service");
            _serviceProvider.Start(ServiceProvider.SERVICE_NAME);
        }

        private void StartWinform(string installationFolder)
        {
            Start(installationFolder, "Streamarr".ProcessNameToExe());
        }

        private void StartConsole(string installationFolder)
        {
            Start(installationFolder, "Streamarr.Console".ProcessNameToExe());
        }

        private void Start(string installationFolder, string fileName)
        {
            _logger.Info("Starting {0}", fileName);
            var path = Path.Combine(installationFolder, fileName);

            if (!_startupContext.Flags.Contains(StartupContext.NO_BROWSER))
            {
                _startupContext.Flags.Add(StartupContext.NO_BROWSER);
            }

            _processProvider.SpawnNewProcess(path, _startupContext.PreservedArguments);
        }
    }
}
