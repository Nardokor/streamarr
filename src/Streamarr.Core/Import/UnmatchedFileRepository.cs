using System.Collections.Generic;
using System.Linq;
using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Import
{
    public interface IUnmatchedFileRepository : IBasicRepository<UnmatchedFile>
    {
        List<UnmatchedFile> GetByCreatorId(int creatorId);
        UnmatchedFile FindByFilePath(string filePath);
    }

    public class UnmatchedFileRepository : BasicRepository<UnmatchedFile>, IUnmatchedFileRepository
    {
        public UnmatchedFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<UnmatchedFile> GetByCreatorId(int creatorId)
        {
            return Query(f => f.CreatorId == creatorId);
        }

        public UnmatchedFile FindByFilePath(string filePath)
        {
            return Query(f => f.FilePath == filePath).SingleOrDefault();
        }
    }
}
