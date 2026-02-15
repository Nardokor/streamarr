using System;
using Moq;
using NUnit.Framework;
using Streamarr.Common;
using Streamarr.Common.EnvironmentInfo;
using Streamarr.Common.Extensions;
using Streamarr.Common.Processes;
using Streamarr.Test.Common;
using Streamarr.Update.UpdateEngine;
using IServiceProvider = Streamarr.Common.IServiceProvider;

namespace Streamarr.Update.Test
{
    [TestFixture]
    public class StartStreamarrServiceFixture : TestBase<StartStreamarr>
    {
        [Test]
        public void should_start_service_if_app_type_was_serivce()
        {
            var targetFolder = "c:\\Sonarr\\".AsOsAgnostic();

            Subject.Start(AppType.Service, targetFolder);

            Mocker.GetMock<IServiceProvider>().Verify(c => c.Start(ServiceProvider.SERVICE_NAME), Times.Once());
        }

        [Test]
        public void should_start_console_if_app_type_was_service_but_start_failed_because_of_permissions()
        {
            var targetFolder = "c:\\Sonarr\\".AsOsAgnostic();
            var targetProcess = "c:\\Sonarr\\Sonarr.Console".AsOsAgnostic().ProcessNameToExe();

            Mocker.GetMock<IServiceProvider>().Setup(c => c.Start(ServiceProvider.SERVICE_NAME)).Throws(new InvalidOperationException());

            Subject.Start(AppType.Service, targetFolder);

            Mocker.GetMock<IProcessProvider>().Verify(c => c.SpawnNewProcess(targetProcess, "/" + StartupContext.NO_BROWSER, null, false), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
