using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Core.Configuration;
using Streamarr.Core.Languages;
using Streamarr.Core.Localization;
using Streamarr.Core.Test.Framework;
using Streamarr.Test.Common;

namespace Streamarr.Core.Test.Localization
{
    [TestFixture]
    public class LocalizationServiceFixture : CoreTest<LocalizationService>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigService>().Setup(m => m.UILanguage).Returns((int)Language.English);

            Mocker.GetMock<IAppFolderInfo>().Setup(m => m.StartUpFolder).Returns(TestContext.CurrentContext.TestDirectory);
        }

        [Test]
        public void should_get_string_in_dictionary_if_lang_exists_and_string_exists()
        {
            var localizedString = Subject.GetLocalizedString("UiLanguage");

            localizedString.Should().Be("UI Language");
        }

        [Test]
        public void should_get_string_in_french()
        {
            Mocker.GetMock<IConfigService>().Setup(m => m.UILanguage).Returns((int)Language.French);

            var localizedString = Subject.GetLocalizedString("UiLanguage");

            localizedString.Should().Be("Langue de l'interface utilisateur");

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_get_string_in_default_dictionary_if_unknown_language_and_string_exists()
        {
            Mocker.GetMock<IConfigService>().Setup(m => m.UILanguage).Returns(0);
            var localizedString = Subject.GetLocalizedString("UiLanguage");

            localizedString.Should().Be("UI Language");
        }

        [Test]
        public void should_return_argument_if_string_doesnt_exists()
        {
            var localizedString = Subject.GetLocalizedString("badString");

            localizedString.Should().Be("badString");
        }

        [Test]
        public void should_return_argument_if_string_doesnt_exists_default_lang()
        {
            var localizedString = Subject.GetLocalizedString("badString");

            localizedString.Should().Be("badString");
        }

        [Test]
        public void should_throw_if_empty_string_passed()
        {
            Assert.Throws<ArgumentNullException>(() => Subject.GetLocalizedString(""));
        }

        [Test]
        public void should_throw_if_null_string_passed()
        {
            Assert.Throws<ArgumentNullException>(() => Subject.GetLocalizedString(null));
        }

        [Test]
        public void should_return_phrase_key_when_localization_file_deserializes_to_null()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var localizationDir = Path.Combine(tempDir, "Localization", "Core");
            Directory.CreateDirectory(localizationDir);

            // JsonSerializer.Deserialize returns null when the JSON content is the literal "null"
            File.WriteAllText(Path.Combine(localizationDir, "en.json"), "null");

            Mocker.GetMock<IAppFolderInfo>().Setup(m => m.StartUpFolder).Returns(tempDir);

            try
            {
                var result = Subject.GetLocalizedString("UiLanguage");
                result.Should().Be("UiLanguage");
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
