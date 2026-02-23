using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Streamarr.Core.Qualities;

namespace Streamarr.Core.History
{
    public interface IDownloadHistoryService
    {
        void Record(
            int contentId,
            int channelId,
            int creatorId,
            string title,
            QualityModel quality,
            DownloadHistoryEventType eventType,
            string data = "");

        List<DownloadHistory> GetAll();
        List<DownloadHistory> GetByCreatorId(int creatorId);
    }

    public class DownloadHistoryService : IDownloadHistoryService
    {
        private readonly IDownloadHistoryRepository _repo;
        private readonly Logger _logger;

        public DownloadHistoryService(IDownloadHistoryRepository repo, Logger logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public void Record(
            int contentId,
            int channelId,
            int creatorId,
            string title,
            QualityModel quality,
            DownloadHistoryEventType eventType,
            string data = "")
        {
            _logger.Debug("Recording history: {0} for content {1}", eventType, contentId);

            _repo.Insert(new DownloadHistory
            {
                ContentId = contentId,
                ChannelId = channelId,
                CreatorId = creatorId,
                Title = title,
                Quality = quality ?? new QualityModel(),
                EventType = eventType,
                Data = data,
                Date = DateTime.UtcNow,
            });
        }

        public List<DownloadHistory> GetAll()
        {
            return _repo.All().ToList();
        }

        public List<DownloadHistory> GetByCreatorId(int creatorId)
        {
            return _repo.GetByCreatorId(creatorId);
        }
    }
}
