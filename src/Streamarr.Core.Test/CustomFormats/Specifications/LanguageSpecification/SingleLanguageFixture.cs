using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Common.Extensions;
using Streamarr.Core.CustomFormats;
using Streamarr.Core.Languages;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Test.Framework;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Test.CustomFormats.Specifications.LanguageSpecification
{
    [TestFixture]
    public class SingleLanguageFixture : CoreTest<Core.CustomFormats.LanguageSpecification>
    {
        private CustomFormatInput _input;

        [SetUp]
        public void Setup()
        {
            _input = new CustomFormatInput
            {
                EpisodeInfo = Builder<ParsedEpisodeInfo>.CreateNew().Build(),
                Series = Builder<Series>.CreateNew().With(s => s.OriginalLanguage = Language.English).Build(),
                Size = 100.Megabytes(),
                Languages = new List<Language>
                {
                    Language.French
                },
                Filename = "Series.Title.S01E01"
            };
        }

        [Test]
        public void should_match_same_language()
        {
            Subject.Value = Language.French.Id;
            Subject.Negate = false;

            Subject.IsSatisfiedBy(_input).Should().BeTrue();
        }

        [Test]
        public void should_not_match_different_language()
        {
            Subject.Value = Language.Spanish.Id;
            Subject.Negate = false;

            Subject.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [Test]
        public void should_not_match_negated_same_language()
        {
            Subject.Value = Language.French.Id;
            Subject.Negate = true;

            Subject.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [Test]
        public void should_match_negated_different_language()
        {
            Subject.Value = Language.Spanish.Id;
            Subject.Negate = true;

            Subject.IsSatisfiedBy(_input).Should().BeTrue();
        }

        [Test]
        public void should_match_negated_except_language_if_language_is_only_present_language()
        {
            Subject.Value = Language.French.Id;
            Subject.ExceptLanguage = true;
            Subject.Negate = true;

            Subject.IsSatisfiedBy(_input).Should().BeTrue();
        }
    }
}
