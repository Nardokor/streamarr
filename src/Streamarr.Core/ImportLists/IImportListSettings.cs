using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.ImportLists
{
    public interface IImportListSettings : IProviderConfig
    {
        string BaseUrl { get; set; }
    }
}
