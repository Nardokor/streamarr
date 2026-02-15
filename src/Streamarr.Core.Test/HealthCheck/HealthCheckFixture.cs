using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.HealthCheck;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.HealthCheck
{
    [TestFixture]
    public class HealthCheckFixture : CoreTest
    {
        private const string WikiRoot = "https://wiki.servarr.com/";
        [TestCase("I blew up because of some weird user mistake", null, WikiRoot + "sonarr/system#i-blew-up-because-of-some-weird-user-mistake")]
        [TestCase("I blew up because of some weird user mistake", "#my-health-check", WikiRoot + "sonarr/system#my-health-check")]
        [TestCase("I blew up because of some weird user mistake", "custom_page#my-health-check", WikiRoot + "sonarr/custom_page#my-health-check")]
        public void should_format_wiki_url(string message, string wikiFragment, string expectedUrl)
        {
            var subject = new Streamarr.Core.HealthCheck.HealthCheck(typeof(HealthCheckBase), HealthCheckResult.Warning, HealthCheckReason.ServerNotification, message, wikiFragment);

            subject.WikiUrl.Should().Be(expectedUrl);
        }
    }
}
