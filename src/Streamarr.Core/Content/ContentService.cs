using System.Collections.Generic;
using NLog;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Content
{
    public interface IContentService
    {
        Content GetContent(int contentId);
        List<Content> GetByChannelId(int channelId);
        Content FindByPlatformContentId(int channelId, string platformContentId);
        List<Content> GetMissingContent(int channelId);
        List<Content> GetAllMissing();
        List<Content> GetAllDownloaded();
        List<Content> GetAllLiveNow();
        List<Content> GetAllRecording();
        List<Content> GetAllWanted();
        Content AddContent(Content content);
        void AddContents(List<Content> contents);
        Content UpdateContent(Content content);
        void DeleteContent(int contentId);
        void DeleteByChannelId(int channelId);
    }

    public class ContentService : IContentService
    {
        private readonly IContentRepository _repo;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public ContentService(IContentRepository repo,
                              IEventAggregator eventAggregator,
                              Logger logger)
        {
            _repo = repo;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public Content GetContent(int contentId)
        {
            return _repo.Get(contentId);
        }

        public List<Content> GetByChannelId(int channelId)
        {
            return _repo.GetByChannelId(channelId);
        }

        public Content FindByPlatformContentId(int channelId, string platformContentId)
        {
            return _repo.FindByPlatformContentId(channelId, platformContentId);
        }

        public List<Content> GetMissingContent(int channelId)
        {
            return _repo.GetWithoutFiles(channelId);
        }

        public List<Content> GetAllMissing()
        {
            return _repo.GetAllMissing();
        }

        public List<Content> GetAllDownloaded()
        {
            return _repo.GetAllDownloaded();
        }

        public List<Content> GetAllLiveNow()
        {
            return _repo.GetAllLiveNow();
        }

        public List<Content> GetAllRecording()
        {
            return _repo.GetAllRecording();
        }

        public List<Content> GetAllWanted()
        {
            return _repo.GetAllWanted();
        }

        public Content AddContent(Content content)
        {
            _logger.Debug("Adding content '{0}' for channel {1}", content.Title, content.ChannelId);
            return _repo.Insert(content);
        }

        public void AddContents(List<Content> contents)
        {
            _repo.InsertMany(contents);
        }

        public Content UpdateContent(Content content)
        {
            return _repo.Update(content);
        }

        public void DeleteContent(int contentId)
        {
            _repo.Delete(contentId);
        }

        public void DeleteByChannelId(int channelId)
        {
            var contents = _repo.GetByChannelId(channelId);
            _repo.DeleteMany(contents);
        }
    }
}
