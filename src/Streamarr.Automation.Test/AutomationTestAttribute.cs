using NUnit.Framework;

namespace Streamarr.Automation.Test
{
    public class AutomationTestAttribute : CategoryAttribute
    {
        public AutomationTestAttribute()
            : base("AutomationTest")
        {
        }
    }
}
