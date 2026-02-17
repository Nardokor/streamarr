using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using Streamarr.Core.Lifecycle;
using Streamarr.Core.Profiles.Qualities;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Profiles
{
    [TestFixture]
    public class QualityProfileServiceFixture : CoreTest<QualityProfileService>
    {
        [Test]
        public void init_should_add_default_profiles()
        {
            Subject.Handle(new ApplicationStartedEvent());

            Mocker.GetMock<IQualityProfileRepository>()
                .Verify(v => v.Insert(It.IsAny<QualityProfile>()), Times.Exactly(6));
        }

        [Test]

        // This confirms that new profiles are added only if no other profiles exists.
        // We don't want to keep adding them back if a user deleted them on purpose.
        public void Init_should_skip_if_any_profiles_already_exist()
        {
            Mocker.GetMock<IQualityProfileRepository>()
                  .Setup(s => s.All())
                  .Returns(Builder<QualityProfile>.CreateListOfSize(2).Build().ToList());

            Subject.Handle(new ApplicationStartedEvent());

            Mocker.GetMock<IQualityProfileRepository>()
                .Verify(v => v.Insert(It.IsAny<QualityProfile>()), Times.Never());
        }

        [Test]
        public void should_delete_profile()
        {
            Subject.Delete(1);

            Mocker.GetMock<IQualityProfileRepository>().Verify(c => c.Delete(1), Times.Once());
        }
    }
}
