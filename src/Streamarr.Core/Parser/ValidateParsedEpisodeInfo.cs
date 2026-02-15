using NLog;
using Streamarr.Common.Instrumentation;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Parser
{
    public static class ValidateParsedEpisodeInfo
    {
        private static readonly Logger Logger = StreamarrLogger.GetLogger(typeof(ValidateParsedEpisodeInfo));

        public static bool ValidateForSeriesType(ParsedEpisodeInfo parsedEpisodeInfo, Series series, bool warnIfInvalid = true)
        {
            if (parsedEpisodeInfo.IsDaily && series.SeriesType == SeriesTypes.Standard)
            {
                var message = $"Found daily-style episode for non-daily series: {series}";

                if (warnIfInvalid)
                {
                    Logger.Warn(message);
                }
                else
                {
                    Logger.Debug(message);
                }

                return false;
            }

            return true;
        }
    }
}
