using NUnit.Framework;

namespace Streamarr.Test.Common.Categories
{
    public class ManualTestAttribute : CategoryAttribute
    {
        public ManualTestAttribute()
            : base("ManualTest")
        {
        }
    }
}
