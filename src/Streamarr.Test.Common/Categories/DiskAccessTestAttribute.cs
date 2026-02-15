using NUnit.Framework;

namespace Streamarr.Test.Common.Categories
{
    public class DiskAccessTestAttribute : CategoryAttribute
    {
        public DiskAccessTestAttribute()
            : base("DiskAccessTest")
        {
        }
    }
}
