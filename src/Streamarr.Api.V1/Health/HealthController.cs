using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Datastore.Events;
using Streamarr.Core.HealthCheck;
using Streamarr.Core.Messaging.Events;
using Streamarr.Http;
using Streamarr.Http.REST;
using Streamarr.SignalR;

namespace Streamarr.Api.V1.Health;

[V1ApiController]
public class HealthController : RestControllerWithSignalR<HealthResource, HealthCheck>,
                            IHandle<HealthCheckCompleteEvent>
{
    private readonly IHealthCheckService _healthCheckService;

    public HealthController(IBroadcastSignalRMessage signalRBroadcaster, IHealthCheckService healthCheckService)
        : base(signalRBroadcaster)
    {
        _healthCheckService = healthCheckService;
    }

    [NonAction]
    public override ActionResult<HealthResource> GetResourceByIdWithErrorHandler(int id)
    {
        return base.GetResourceByIdWithErrorHandler(id);
    }

    protected override HealthResource GetResourceById(int id)
    {
        throw new NotImplementedException();
    }

    [HttpGet]
    [Produces("application/json")]
    public List<HealthResource> GetHealth()
    {
        return _healthCheckService.Results().ToResource();
    }

    [NonAction]
    public void Handle(HealthCheckCompleteEvent message)
    {
        BroadcastResourceChange(ModelAction.Sync);
    }
}
