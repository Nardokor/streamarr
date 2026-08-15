using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Import;
using Streamarr.Http;

namespace Streamarr.Api.V1.Import;

[V1ApiController("import")]
public class ImportController : Controller
{
    private readonly IImportLibraryService _importService;
    private readonly IUrlImportService _urlImportService;

    public ImportController(IImportLibraryService importService, IUrlImportService urlImportService)
    {
        _importService = importService;
        _urlImportService = urlImportService;
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

    [HttpPost("url")]
    [Produces("application/json")]
    public UrlImportResult ImportUrl([FromBody] UrlImportRequest request)
    {
        return _urlImportService.Import(request.Url, request.ChannelId);
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

public class UrlImportRequest
{
    public string Url { get; set; } = string.Empty;
    public int? ChannelId { get; set; }
}
