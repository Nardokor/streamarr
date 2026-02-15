using System.Collections.Generic;
using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.Indexers
{
    public interface IParseIndexerResponse
    {
        IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse);
    }
}
