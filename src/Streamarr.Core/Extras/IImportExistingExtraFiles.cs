using System.Collections.Generic;
using Streamarr.Core.Extras.Files;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Extras
{
    public interface IImportExistingExtraFiles
    {
        int Order { get; }
        IEnumerable<ExtraFile> ProcessFiles(Series series, List<string> filesOnDisk, List<string> importedFiles, string fileNameBeforeRename);
    }
}
