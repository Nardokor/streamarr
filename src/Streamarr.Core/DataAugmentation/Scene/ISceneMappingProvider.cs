using System.Collections.Generic;

namespace Streamarr.Core.DataAugmentation.Scene
{
    public interface ISceneMappingProvider
    {
        List<SceneMapping> GetSceneMappings();
    }
}
