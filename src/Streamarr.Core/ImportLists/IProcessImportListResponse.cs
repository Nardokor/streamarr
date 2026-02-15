using System.Collections.Generic;
using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.ImportLists
{
    public interface IParseImportListResponse
    {
        IList<ImportListItemInfo> ParseResponse(ImportListResponse importListResponse);
    }
}
