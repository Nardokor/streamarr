using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Streamarr.Common.Disk;
using Streamarr.Core.Creators;
using Streamarr.Core.Download;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Download
{
    [TestFixture]
    public class OrphanedFileScannerFixture : CoreTest<OrphanedFileScanner>
    {
        private Creator _creator;

        [SetUp]
        public void SetUp()
        {
            _creator = new Creator { Id = 1, Title = "Test Creator", Path = "/data/Test Creator" };

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.GetAllCreators())
                  .Returns(new List<Creator> { _creator });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.FolderExists(_creator.Path))
                  .Returns(true);
        }

        [Test]
        public void scan_returns_empty_when_no_files_present()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.GetFiles(_creator.Path, true))
                  .Returns(new List<string>());

            var result = Subject.Scan();

            result.Should().BeEmpty();
        }

        [Test]
        public void scan_skips_creator_folder_that_does_not_exist()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.FolderExists(_creator.Path))
                  .Returns(false);

            var result = Subject.Scan();

            result.Should().BeEmpty();
            Mocker.GetMock<IDiskProvider>().Verify(d => d.GetFiles(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public void scan_ignores_finished_video_files()
        {
            var filePath = $"{_creator.Path}/Video Title [abc123].mp4";

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.GetFiles(_creator.Path, true))
                  .Returns(new List<string> { filePath });

            var result = Subject.Scan();

            result.Should().BeEmpty();
        }

        [TestCase("Video Title [abc123].mp4.part")]
        [TestCase("Video Title [abc123].f137.mp4")]
        [TestCase("Video Title [abc123].ytdl")]
        [TestCase("Video Title [abc123].temp")]
        public void scan_flags_intermediate_files(string fileName)
        {
            var filePath = $"{_creator.Path}/{fileName}";

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.GetFiles(_creator.Path, true))
                  .Returns(new List<string> { filePath });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.FileGetLastWrite(filePath))
                  .Returns(DateTime.UtcNow);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.GetFileSize(filePath))
                  .Returns(1024);

            var result = Subject.Scan();

            result.Should().ContainSingle();
            result[0].Path.Should().Be(filePath);
            result[0].CreatorId.Should().Be(_creator.Id);
        }

        [Test]
        public void scan_marks_old_files_as_stale()
        {
            var filePath = $"{_creator.Path}/Video Title [abc123].mp4.part";

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.GetFiles(_creator.Path, true))
                  .Returns(new List<string> { filePath });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.FileGetLastWrite(filePath))
                  .Returns(DateTime.UtcNow.AddHours(-5));

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.GetFileSize(filePath))
                  .Returns(1024);

            var result = Subject.Scan();

            result.Should().ContainSingle();
            result[0].Stale.Should().BeTrue();
        }

        [Test]
        public void scan_does_not_mark_recent_files_as_stale()
        {
            var filePath = $"{_creator.Path}/Video Title [abc123].mp4.part";

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.GetFiles(_creator.Path, true))
                  .Returns(new List<string> { filePath });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.FileGetLastWrite(filePath))
                  .Returns(DateTime.UtcNow);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.GetFileSize(filePath))
                  .Returns(1024);

            var result = Subject.Scan();

            result.Should().ContainSingle();
            result[0].Stale.Should().BeFalse();
        }

        [Test]
        public void delete_removes_file_under_known_creator_path()
        {
            var filePath = $"{_creator.Path}/Video Title [abc123].mp4.part";

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.FileExists(filePath))
                  .Returns(true);

            Subject.Delete(filePath);

            Mocker.GetMock<IDiskProvider>().Verify(d => d.DeleteFile(filePath), Times.Once);
        }

        [Test]
        public void delete_throws_for_path_outside_known_creator_folders()
        {
            var action = () => Subject.Delete("/etc/passwd");

            action.Should().Throw<InvalidOperationException>();
            Mocker.GetMock<IDiskProvider>().Verify(d => d.DeleteFile(It.IsAny<string>()), Times.Never);
        }
    }
}
