using System.Linq;

namespace Streamarr.Core.Organizer
{
    public interface INamingConfigService
    {
        NamingConfig GetConfig();
        void Save(NamingConfig config);
    }

    public class NamingConfigService : INamingConfigService
    {
        private readonly INamingConfigRepository _repo;

        public NamingConfigService(INamingConfigRepository repo)
        {
            _repo = repo;
        }

        public NamingConfig GetConfig()
        {
            var config = _repo.All().FirstOrDefault();

            if (config == null)
            {
                config = NamingConfig.Default;
                config = _repo.Insert(config);
            }

            return config;
        }

        public void Save(NamingConfig config)
        {
            _repo.Upsert(config);
        }
    }
}
