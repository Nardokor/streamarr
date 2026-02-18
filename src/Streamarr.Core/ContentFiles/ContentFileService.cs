using System.Collections.Generic;
using NLog;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.ContentFiles
{
    public interface IContentFileService
    {
        ContentFile GetContentFile(int contentFileId);
        ContentFile FindByContentId(int contentId);
        List<ContentFile> GetByContentId(int contentId);
        ContentFile AddContentFile(ContentFile contentFile);
        ContentFile UpdateContentFile(ContentFile contentFile);
        void DeleteContentFile(int contentFileId);
    }

    public class ContentFileService : IContentFileService
    {
        private readonly IContentFileRepository _repo;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public ContentFileService(IContentFileRepository repo,
                                  IEventAggregator eventAggregator,
                                  Logger logger)
        {
            _repo = repo;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public ContentFile GetContentFile(int contentFileId)
        {
            return _repo.Get(contentFileId);
        }

        public ContentFile FindByContentId(int contentId)
        {
            return _repo.FindByContentId(contentId);
        }

        public List<ContentFile> GetByContentId(int contentId)
        {
            return _repo.GetByContentId(contentId);
        }

        public ContentFile AddContentFile(ContentFile contentFile)
        {
            _logger.Debug("Adding content file '{0}'", contentFile.RelativePath);
            return _repo.Insert(contentFile);
        }

        public ContentFile UpdateContentFile(ContentFile contentFile)
        {
            return _repo.Update(contentFile);
        }

        public void DeleteContentFile(int contentFileId)
        {
            _repo.Delete(contentFileId);
        }
    }
}
