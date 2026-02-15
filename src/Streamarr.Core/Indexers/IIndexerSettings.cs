using System.Collections.Generic;
using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.Indexers
{
    public interface IIndexerSettings : IProviderConfig
    {
        string BaseUrl { get; set; }

        IEnumerable<int> MultiLanguages { get; set; }

        IEnumerable<int> FailDownloads { get; set; }
    }
}
