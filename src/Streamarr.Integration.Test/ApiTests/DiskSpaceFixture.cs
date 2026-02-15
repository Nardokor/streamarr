using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Integration.Test.Client;
using Streamarr.Api.V3.DiskSpace;

namespace Streamarr.Integration.Test.ApiTests
{
    [TestFixture]
    public class DiskSpaceFixture : IntegrationTest
    {
        public ClientBase<DiskSpaceResource> DiskSpace;

        protected override void InitRestClients()
        {
            base.InitRestClients();

            DiskSpace = new ClientBase<DiskSpaceResource>(RestClient, ApiKey, "diskSpace");
        }

        [Test]
        [Ignore("Fails on build agent")]
        public void get_all_diskspace()
        {
            var items = DiskSpace.All();

            items.Should().NotBeEmpty();
            items.First().FreeSpace.Should().NotBe(0);
            items.First().TotalSpace.Should().NotBe(0);
            items.First().Path.Should().NotBeNullOrEmpty();
        }
    }
}
