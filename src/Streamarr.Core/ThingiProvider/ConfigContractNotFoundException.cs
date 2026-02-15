using Streamarr.Common.Exceptions;

namespace Streamarr.Core.ThingiProvider
{
    public class ConfigContractNotFoundException : StreamarrException
    {
        public ConfigContractNotFoundException(string contract)
            : base("Couldn't find config contract " + contract)
        {
        }
    }
}
