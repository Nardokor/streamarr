using System.Collections.Generic;
using System.Linq;
using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.ContentFiles
{
    public interface IContentFileRepository : IBasicRepository<ContentFile>
    {
        List<ContentFile> GetByContentId(int contentId);
        ContentFile FindByContentId(int contentId);
    }

    public class ContentFileRepository : BasicRepository<ContentFile>, IContentFileRepository
    {
        public ContentFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        protected override bool PublishModelEvents => true;

        public List<ContentFile> GetByContentId(int contentId)
        {
            return Query(cf => cf.ContentId == contentId);
        }

        public ContentFile FindByContentId(int contentId)
        {
            return Query(cf => cf.ContentId == contentId).SingleOrDefault();
        }
    }
}
