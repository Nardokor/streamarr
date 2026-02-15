using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.RemotePathMappings
{
    public interface IRemotePathMappingRepository : IBasicRepository<RemotePathMapping>
    {
    }

    public class RemotePathMappingRepository : BasicRepository<RemotePathMapping>, IRemotePathMappingRepository
    {
        public RemotePathMappingRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        protected override bool PublishModelEvents => true;
    }
}
