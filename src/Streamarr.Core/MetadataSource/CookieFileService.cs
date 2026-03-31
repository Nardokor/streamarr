using System.IO;
using NLog;
using Streamarr.Common.EnvironmentInfo;

namespace Streamarr.Core.MetadataSource
{
    public class CookieFileService : ICookieFileService
    {
        private readonly string _cookiesFolder;
        private readonly Logger _logger;

        public CookieFileService(IAppFolderInfo appFolderInfo, Logger logger)
        {
            _cookiesFolder = Path.Combine(appFolderInfo.AppDataFolder, "cookies");
            _logger = logger;
        }

        public string GetPath(int definitionId) =>
            Path.Combine(_cookiesFolder, $"{definitionId}.txt");

        public string Save(int definitionId, byte[] content)
        {
            Directory.CreateDirectory(_cookiesFolder);
            var path = GetPath(definitionId);
            File.WriteAllBytes(path, content);
            _logger.Debug("Saved cookie file for source {0} at {1}", definitionId, path);
            return path;
        }

        public void Delete(int definitionId)
        {
            var path = GetPath(definitionId);
            if (File.Exists(path))
            {
                File.Delete(path);
                _logger.Debug("Deleted cookie file for source {0}", definitionId);
            }
        }

        public bool Exists(int definitionId) => File.Exists(GetPath(definitionId));
    }
}
