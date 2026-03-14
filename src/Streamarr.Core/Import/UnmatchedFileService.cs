using System.Collections.Generic;
using System.Linq;
using NLog;

namespace Streamarr.Core.Import
{
    public interface IUnmatchedFileService
    {
        List<UnmatchedFile> GetAll();
        List<UnmatchedFile> GetByCreatorId(int creatorId);
        UnmatchedFile Add(UnmatchedFile unmatchedFile);
        void Delete(int id);
    }

    public class UnmatchedFileService : IUnmatchedFileService
    {
        private readonly IUnmatchedFileRepository _repo;
        private readonly Logger _logger;

        public UnmatchedFileService(IUnmatchedFileRepository repo, Logger logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public List<UnmatchedFile> GetAll()
        {
            return _repo.All().ToList();
        }

        public List<UnmatchedFile> GetByCreatorId(int creatorId)
        {
            return _repo.GetByCreatorId(creatorId);
        }

        public UnmatchedFile Add(UnmatchedFile unmatchedFile)
        {
            var existing = _repo.FindByFilePath(unmatchedFile.FilePath);
            if (existing != null)
            {
                return existing;
            }

            _logger.Debug("Recording unmatched file '{0}' (reason: {1})", unmatchedFile.FileName, unmatchedFile.Reason);
            return _repo.Insert(unmatchedFile);
        }

        public void Delete(int id)
        {
            _repo.Delete(id);
        }
    }
}
