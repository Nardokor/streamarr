using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Streamarr.Common.Disk;
using Streamarr.Common.Extensions;
using Streamarr.Core.Configuration;
using Streamarr.Core.DecisionEngine.Specifications;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Test.Framework;
using Streamarr.Core.Tv;
using Streamarr.Test.Common;

namespace Streamarr.Core.Test.DecisionEngineTests
{
    public class FreeSpaceSpecificationFixture : CoreTest<FreeSpaceSpecification>
    {
        private RemoteEpisode _remoteEpisode;

        [SetUp]
        public void Setup()
        {
            _remoteEpisode = new RemoteEpisode() { Release = new ReleaseInfo(), Series = new Series { Path = @"C:\Test\TV\Series".AsOsAgnostic() } };
        }

        private void WithMinimumFreeSpace(int size)
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.MinimumFreeSpaceWhenImporting).Returns(size);
        }

        private void WithAvailableSpace(int size)
        {
            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetAvailableSpace(It.IsAny<string>())).Returns(size.Megabytes());
        }

        private void WithSize(int size)
        {
            _remoteEpisode.Release.Size = size.Megabytes();
        }

        [Test]
        public void should_return_true_when_available_space_is_more_than_size()
        {
            WithMinimumFreeSpace(0);
            WithAvailableSpace(200);
            WithSize(100);

            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_available_space_minus_size_is_more_than_minimum_free_space()
        {
            WithMinimumFreeSpace(50);
            WithAvailableSpace(200);
            WithSize(100);

            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_available_space_is_less_than_size()
        {
            WithMinimumFreeSpace(0);
            WithAvailableSpace(200);
            WithSize(1000);

            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_available_space_minus_size_is_less_than_minimum_free_space()
        {
            WithMinimumFreeSpace(150);
            WithAvailableSpace(200);
            WithSize(100);

            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_skip_free_space_check_is_true()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.SkipFreeSpaceCheckWhenImporting)
                .Returns(true);

            WithMinimumFreeSpace(150);
            WithAvailableSpace(200);
            WithSize(100);

            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_root_folder_is_not_available()
        {
            WithMinimumFreeSpace(150);
            WithSize(100);

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetAvailableSpace(It.IsAny<string>())).Throws<DirectoryNotFoundException>();

            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeTrue();
        }
    }
}
