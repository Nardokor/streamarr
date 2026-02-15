using System.Text;
using NLog;
using NLog.Layouts.ClefJsonLayout;
using Streamarr.Common.EnvironmentInfo;

namespace Streamarr.Common.Instrumentation;

public class CleansingClefLogLayout : CompactJsonLayout
{
    protected override void RenderFormattedMessage(LogEventInfo logEvent, StringBuilder target)
    {
        base.RenderFormattedMessage(logEvent, target);

        if (RuntimeInfo.IsProduction)
        {
            var result = CleanseLogMessage.Cleanse(target.ToString());
            target.Clear();
            target.Append(result);
        }
    }
}
