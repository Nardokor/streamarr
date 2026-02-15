using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using Streamarr.Core.Housekeeping.Housekeepers;
using Streamarr.Core.Test.Framework;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class UpdateCleanTitleForSeriesFixture : CoreTest<UpdateCleanTitleForSeries>
    {
        [Test]
        public void should_update_clean_title()
        {
            var series = Builder<Series>.CreateNew()
                                        .With(s => s.Title = "Full Title")
                                        .With(s => s.CleanTitle = "unclean")
                                        .Build();

            Mocker.GetMock<ISeriesRepository>()
                 .Setup(s => s.All())
                 .Returns(new[] { series });

            Subject.Clean();

            Mocker.GetMock<ISeriesRepository>()
                .Verify(v => v.Update(It.Is<Series>(s => s.CleanTitle == "fulltitle")), Times.Once());
        }

        [Test]
        public void should_not_update_unchanged_title()
        {
            var series = Builder<Series>.CreateNew()
                                        .With(s => s.Title = "Full Title")
                                        .With(s => s.CleanTitle = "fulltitle")
                                        .Build();

            Mocker.GetMock<ISeriesRepository>()
                 .Setup(s => s.All())
                 .Returns(new[] { series });

            Subject.Clean();

            Mocker.GetMock<ISeriesRepository>()
                .Verify(v => v.Update(It.Is<Series>(s => s.CleanTitle == "fulltitle")), Times.Never());
        }
    }
}
