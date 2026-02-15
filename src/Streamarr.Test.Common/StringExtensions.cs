using System.IO;
using Streamarr.Common.EnvironmentInfo;

namespace Streamarr.Test.Common
{
    public static class StringExtensions
    {
        public static string AsOsAgnostic(this string path)
        {
            if (OsInfo.IsNotWindows)
            {
                if (path.Length > 2 && path[1] == ':')
                {
                    path = path.Replace(":", "");
                    path = Path.DirectorySeparatorChar + path;
                }

                path = path.Replace("\\", Path.DirectorySeparatorChar.ToString());
            }

            return path;
        }
    }
}
