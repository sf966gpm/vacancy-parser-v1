using Microsoft.AspNetCore.Mvc;
using Vacancy.Api.Features.HhPresets;

namespace Vacancy.Api.Controllers;

[ApiController]
[Route("api/hh/presets")]
public sealed class HhPresetsController(IHhSearchPresetService presetService) : ControllerBase
{
    /// <summary>Lists saved HH search presets.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HhSearchPresetResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HhSearchPresetResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await presetService.GetAllAsync(cancellationToken));
    }

    /// <summary>Gets a saved HH search preset by key.</summary>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(HhSearchPresetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HhSearchPresetResponse>> GetByKey(
        string key,
        CancellationToken cancellationToken)
    {
        var preset = await presetService.GetByKeyAsync(key, cancellationToken);
        return preset is null ? NotFound() : Ok(preset);
    }

    /// <summary>Creates a saved HH search preset.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(HhSearchPresetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HhSearchPresetResponse>> Create(
        CreateHhSearchPresetRequest request,
        CancellationToken cancellationToken)
    {
        var preset = await presetService.CreateAsync(request, cancellationToken);
        if (preset is null)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Preset key already exists",
                Detail = $"A preset with key '{request.Key}' already exists."
            });
        }

        return CreatedAtAction(nameof(GetByKey), new { key = preset.Key }, preset);
    }

    /// <summary>Updates a saved HH search preset.</summary>
    [HttpPut("{key}")]
    [ProducesResponseType(typeof(HhSearchPresetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HhSearchPresetResponse>> Update(
        string key,
        UpdateHhSearchPresetRequest request,
        CancellationToken cancellationToken)
    {
        var preset = await presetService.UpdateAsync(key, request, cancellationToken);
        return preset is null ? NotFound() : Ok(preset);
    }

    /// <summary>Deletes a saved HH search preset.</summary>
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string key, CancellationToken cancellationToken)
    {
        return await presetService.DeleteAsync(key, cancellationToken) ? NoContent() : NotFound();
    }
}
