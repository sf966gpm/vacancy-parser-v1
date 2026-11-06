using Microsoft.AspNetCore.Mvc;
using Vacancy.Api.Features.HhJobs;
using Vacancy.Api.Messaging.RabbitMq;

namespace Vacancy.Api.Controllers;

[ApiController]
[Route("api/hh/jobs")]
public sealed class HhJobsController(IHhJobPublisher jobPublisher) : ControllerBase
{
    /// <summary>Queues a vacancy search job for the HH collector.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(HhParseJobAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HhParseJobAcceptedResponse>> Create(
        CreateHhParseJobRequest request,
        CancellationToken cancellationToken)
    {
        await jobPublisher.PublishAsync(request, cancellationToken);
        return Accepted(new HhParseJobAcceptedResponse("queued"));
    }
}
