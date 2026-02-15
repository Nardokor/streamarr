using Streamarr.Core.Datastore;

namespace Streamarr.Core.ThingiProvider
{
    public interface IProviderRepository<TProvider> : IBasicRepository<TProvider>
        where TProvider : ModelBase, new()
    {
// void DeleteImplementations(string implementation);
    }
}
