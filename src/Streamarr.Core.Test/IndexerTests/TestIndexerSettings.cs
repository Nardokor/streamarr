using System;
using System.Collections.Generic;
using Streamarr.Core.Indexers;
using Streamarr.Core.Validation;

namespace Streamarr.Core.Test.IndexerTests
{
    public class TestIndexerSettings : IIndexerSettings
    {
        public StreamarrValidationResult Validate()
        {
            throw new NotImplementedException();
        }

        public string BaseUrl { get; set; }

        public IEnumerable<int> MultiLanguages { get; set; }
        public IEnumerable<int> FailDownloads { get; set; }
    }
}
