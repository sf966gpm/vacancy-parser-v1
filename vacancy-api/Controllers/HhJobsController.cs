using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Vacancy.Api.Features.HhJobs;

namespace Vacancy.Api.Controllers;

[ApiController]
[Route("api/hh/jobs")]
public sealed class HhJobsController(IHhJobService jobService) : ControllerBase
{
    /// <summary>Queues a vacancy search job for the HH collector.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(HhParseJobAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HhParseJobAcceptedResponse>> Create(
        CreateHhParseJobRequest request,
        CancellationToken cancellationToken)
    {
        var job = await jobService.CreateAsync(request, cancellationToken);
        if (job is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Preset not found",
                Detail = $"No HH search preset with key '{request.PresetKey}' exists."
            });
        }

        return AcceptedAtAction(nameof(GetById), new { id = job.Id }, job);
    }

    /// <summary>Lists the most recently created HH parse jobs.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HhParseJobResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HhParseJobResponse>>> GetRecent(
        [FromQuery, Range(1, 100)] int take = 50,
        CancellationToken cancellationToken = default)
    {
        return Ok(await jobService.GetRecentAsync(take, cancellationToken));
    }

    /// <summary>Gets an HH parse job by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HhParseJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HhParseJobResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var job = await jobService.GetByIdAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }
}
