using System;
using System.Collections.Generic;
using NLog;
using Streamarr.Core.Datastore.Events;
using Streamarr.Core.Download;
using Streamarr.Core.Download.Clients;
using Streamarr.Core.Localization;
using Streamarr.Core.RemotePathMappings;
using Streamarr.Core.RootFolders;
using Streamarr.Core.ThingiProvider.Events;

namespace Streamarr.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ProviderUpdatedEvent<IDownloadClient>))]
    [CheckOn(typeof(ProviderDeletedEvent<IDownloadClient>))]
    [CheckOn(typeof(ModelEvent<RootFolder>))]
    [CheckOn(typeof(ModelEvent<RemotePathMapping>))]

    public class DownloadClientRemovesCompletedDownloadsCheck : HealthCheckBase, IProvideHealthCheck
    {
        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly Logger _logger;

        public DownloadClientRemovesCompletedDownloadsCheck(IProvideDownloadClient downloadClientProvider,
                                          Logger logger,
                                          ILocalizationService localizationService)
            : base(localizationService)
        {
            _downloadClientProvider = downloadClientProvider;
            _logger = logger;
        }

        public override HealthCheck Check()
        {
            var clients = _downloadClientProvider.GetDownloadClients(true);

            foreach (var client in clients)
            {
                try
                {
                    var clientName = client.Definition.Name;
                    var status = client.GetStatus();

                    if (status.RemovesCompletedDownloads)
                    {
                        return new HealthCheck(GetType(),
                            HealthCheckResult.Warning,
                            HealthCheckReason.DownloadClientRemovesCompletedDownloads,
                            _localizationService.GetLocalizedString("DownloadClientRemovesCompletedDownloadsHealthCheckMessage", new Dictionary<string, object>
                            {
                                { "downloadClientName", clientName }
                            }),
                            "#download-client-removes-completed-downloads");
                    }
                }
                catch (DownloadClientException ex)
                {
                    _logger.Debug(ex, "Unable to communicate with {0}", client.Definition.Name);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unknown error occurred in DownloadClientHistoryRetentionCheck HealthCheck");
                }
            }

            return new HealthCheck(GetType());
        }
    }
}
