using System.Text.RegularExpressions;

namespace Streamarr.Core.Creators
{
    public static class CreatorTitleNormalizer
    {
        private static readonly Regex CleanRegex = new Regex(@"[^\w\s]", RegexOptions.Compiled);

        public static string CleanCreatorTitle(this string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            return CleanRegex.Replace(title, string.Empty).ToLowerInvariant().Trim();
        }
    }
}
