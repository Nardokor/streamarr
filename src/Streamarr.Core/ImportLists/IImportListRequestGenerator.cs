namespace Streamarr.Core.ImportLists
{
    public interface IImportListRequestGenerator
    {
        ImportListPageableRequestChain GetListItems();
    }
}
