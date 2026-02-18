using System.Linq;
using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Creators
{
    public interface ICreatorRepository : IBasicRepository<Creator>
    {
        bool CreatorPathExists(string path);
        Creator FindByTitle(string cleanTitle);
    }

    public class CreatorRepository : BasicRepository<Creator>, ICreatorRepository
    {
        public CreatorRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        protected override bool PublishModelEvents => true;

        public bool CreatorPathExists(string path)
        {
            return Query(c => c.Path == path).Any();
        }

        public Creator FindByTitle(string cleanTitle)
        {
            return Query(c => c.CleanTitle == cleanTitle).SingleOrDefault();
        }
    }
}
