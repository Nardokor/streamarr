using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Import;
using Streamarr.Http;

namespace Streamarr.Api.V1.Import;

[V1ApiController("import")]
public class ImportController : Controller
{
    private readonly IImportLibraryService _importService;

    public ImportController(IImportLibraryService importService)
    {
        _importService = importService;
    }

    [HttpPost("folders")]
    [Produces("application/json")]
    public List<ImportableFolder> GetFolders([FromBody] ImportFoldersRequest request)
    {
        return _importService.GetImportableFolders(request.RootPath);
    }

    [HttpPost]
    [Produces("application/json")]
    public ImportLibraryResult ImportLibrary([FromBody] ImportLibraryRequest request)
    {
        return _importService.Import(request.RootPath, request.FolderNames);
    }
}

public class ImportFoldersRequest
{
    public string RootPath { get; set; } = string.Empty;
}

public class ImportLibraryRequest
{
    public string RootPath { get; set; } = string.Empty;
    public List<string> FolderNames { get; set; } = new();
}
