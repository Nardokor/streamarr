using Streamarr.Core.Datastore;

namespace Streamarr.Core.Tv
{
    public class Ratings : IEmbeddedDocument
    {
        public int Votes { get; set; }
        public decimal Value { get; set; }
    }
}
