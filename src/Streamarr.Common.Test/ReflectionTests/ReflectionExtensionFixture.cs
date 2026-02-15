using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Common.Reflection;
using Streamarr.Core.Datastore;
using Streamarr.Test.Common;

namespace Streamarr.Common.Test.ReflectionTests
{
    public class ReflectionExtensionFixture : TestBase
    {
        [Test]
        public void should_get_properties_from_models()
        {
            var models = Assembly.Load("Sonarr.Core").ImplementationsOf<ModelBase>();

            foreach (var model in models)
            {
                model.GetSimpleProperties().Should().NotBeEmpty();
            }
        }

        [Test]
        public void should_be_able_to_get_implementations()
        {
            var models = Assembly.Load("Sonarr.Core").ImplementationsOf<ModelBase>();

            models.Should().NotBeEmpty();
        }
    }
}
