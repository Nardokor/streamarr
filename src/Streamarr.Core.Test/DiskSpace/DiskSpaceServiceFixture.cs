using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Streamarr.Common.Disk;
using Streamarr.Core.DiskSpace;
using Streamarr.Core.RootFolders;
using Streamarr.Core.Test.Framework;
using Streamarr.Test.Common;

namespace Streamarr.Core.Test.DiskSpace
{
    [TestFixture]
    public class DiskSpaceServiceFixture : CoreTest<DiskSpaceService>
    {
        private string _rootFolder;
        private string _rootFolder2;

        [SetUp]
        public void SetUp()
        {
            _rootFolder = @"G:\fasdlfsdf".AsOsAgnostic();
            _rootFolder2 = @"G:\fasdlfsdf2".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetMounts())
                  .Returns(new List<IMount>());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetPathRoot(It.IsAny<string>()))
                  .Returns(@"G:\".AsOsAgnostic());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetAvailableSpace(It.IsAny<string>()))
                  .Returns(0);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetTotalSize(It.IsAny<string>()))
                  .Returns(0);

            GivenRootFolders();
        }

        private void GivenRootFolders(params string[] rootFolderPaths)
        {
            Mocker.GetMock<IRootFolderService>()
                .Setup(v => v.All())
                .Returns(rootFolderPaths.Select(p => new RootFolder { Path = p }).ToList());
        }

        private void GivenExistingFolder(string folder)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FolderExists(folder))
                  .Returns(true);
        }

        [Test]
        public void should_check_diskspace_for_root_folders()
        {
            GivenRootFolders(_rootFolder);
            GivenExistingFolder(_rootFolder);

            var freeSpace = Subject.GetFreeSpace();

            freeSpace.Should().NotBeEmpty();
        }

        [Test]
        public void should_check_diskspace_for_same_root_folder_only_once()
        {
            GivenRootFolders(_rootFolder, _rootFolder);
            GivenExistingFolder(_rootFolder);

            var freeSpace = Subject.GetFreeSpace();

            freeSpace.Should().HaveCount(1);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.GetAvailableSpace(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_not_check_diskspace_for_missing_root_folders()
        {
            GivenRootFolders(_rootFolder);

            var freeSpace = Subject.GetFreeSpace();

            freeSpace.Should().BeEmpty();

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.GetAvailableSpace(It.IsAny<string>()), Times.Never());
        }

        [TestCase("/boot")]
        [TestCase("/var/lib/rancher")]
        [TestCase("/var/lib/rancher/volumes")]
        [TestCase("/var/lib/kubelet")]
        [TestCase("/var/lib/docker")]
        [TestCase("/some/place/docker/aufs")]
        [TestCase("/etc/network")]
        [TestCase("/Volumes/.timemachine/ABC123456-A1BC-12A3B45678C9/2025-05-13-181401.backup")]
        public void should_not_check_diskspace_for_irrelevant_mounts(string path)
        {
            var mount = new Mock<IMount>();
            mount.SetupGet(v => v.RootDirectory).Returns(path);
            mount.SetupGet(v => v.DriveType).Returns(System.IO.DriveType.Fixed);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetMounts())
                  .Returns(new List<IMount> { mount.Object });

            var freeSpace = Subject.GetFreeSpace();

            freeSpace.Should().BeEmpty();
        }
    }
}
