using System.IO;
using NLog;
using Streamarr.Common.Disk;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Core.Configuration;

namespace Streamarr.Http.Frontend.Mappers
{
    public class FaviconMapper : StaticResourceMapperBase
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IConfigFileProvider _configFileProvider;

        public FaviconMapper(IAppFolderInfo appFolderInfo, IDiskProvider diskProvider, IConfigFileProvider configFileProvider, Logger logger)
            : base(diskProvider, logger)
        {
            _appFolderInfo = appFolderInfo;
            _configFileProvider = configFileProvider;
        }

        public override string Map(string resourceUrl)
        {
            var fileName = "favicon.ico";

            if (BuildInfo.IsDebug)
            {
                fileName = "favicon-debug.ico";
            }

            var path = Path.Combine("Content", "Images", "Icons", fileName);

            return Path.Combine(_appFolderInfo.StartUpFolder, _configFileProvider.UiFolder, path);
        }

        public override bool CanHandle(string resourceUrl)
        {
            return resourceUrl.Equals("/favicon.ico");
        }
    }
}
