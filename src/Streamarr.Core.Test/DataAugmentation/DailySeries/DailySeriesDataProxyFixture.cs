using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.DataAugmentation.DailySeries;
using Streamarr.Core.Test.Framework;
using Streamarr.Test.Common.Categories;

namespace Streamarr.Core.Test.DataAugmentation.DailySeries
{
    [TestFixture]
    [IntegrationTest]
    public class DailySeriesDataProxyFixture : CoreTest<DailySeriesDataProxy>
    {
        [SetUp]
        public void Setup()
        {
            UseRealHttp();
        }

        [Test]
        public void should_get_list_of_daily_series()
        {
            var list = Subject.GetDailySeriesIds();
            list.Should().NotBeEmpty();
            list.Should().OnlyHaveUniqueItems();
        }
    }
}
