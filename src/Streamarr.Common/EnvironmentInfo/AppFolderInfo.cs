using System;
using System.IO;
using System.Reflection;
using NLog;
using Streamarr.Common.Instrumentation;

namespace Streamarr.Common.EnvironmentInfo
{
    public interface IAppFolderInfo
    {
        string AppDataFolder { get; }
        string LegacyAppDataFolder { get; }
        string TempFolder { get; }
        string StartUpFolder { get; }
    }

    public class AppFolderInfo : IAppFolderInfo
    {
        private readonly Environment.SpecialFolder _dataSpecialFolder = Environment.SpecialFolder.CommonApplicationData;

        private static readonly Logger Logger = StreamarrLogger.GetLogger(typeof(AppFolderInfo));

        public AppFolderInfo(IStartupContext startupContext)
        {
            if (OsInfo.IsNotWindows)
            {
                _dataSpecialFolder = Environment.SpecialFolder.ApplicationData;
            }

            if (startupContext.Args.TryGetValue(StartupContext.APPDATA, out var argsAppDataFolder))
            {
                AppDataFolder = argsAppDataFolder;
                Logger.Info("Data directory is being overridden to [{0}]", AppDataFolder);
            }
            else
            {
                AppDataFolder = Path.Combine(Environment.GetFolderPath(_dataSpecialFolder, Environment.SpecialFolderOption.DoNotVerify), "Streamarr");

                // Only macOS has a legacy folder to migrate from (differing default locations
                // pre/post-rebrand). On other platforms the default location hasn't moved, so
                // there's nothing to migrate — leave this null (MigrateAppDataFolder no-ops on
                // a blank LegacyAppDataFolder).
                LegacyAppDataFolder = OsInfo.IsOsx
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), ".config", "Streamarr")
                    : null;
            }

            StartUpFolder = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            TempFolder = Path.GetTempPath();
        }

        public string AppDataFolder { get; }
        public string LegacyAppDataFolder { get; }
        public string StartUpFolder { get; }
        public string TempFolder { get; }
    }
}
