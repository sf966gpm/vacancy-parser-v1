using Microsoft.AspNetCore.Mvc;
using Vacancy.Api.Features.Presets;

namespace Vacancy.Api.Controllers;

[ApiController]
[Route("api/presets")]
public sealed class SearchPresetsController(ISearchPresetService presetService) : ControllerBase
{
    /// <summary>Lists saved search presets for all platforms.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SearchPresetResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SearchPresetResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await presetService.GetAllAsync(cancellationToken));
    }

    /// <summary>Gets a saved search preset by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SearchPresetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SearchPresetResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var preset = await presetService.GetByIdAsync(id, cancellationToken);
        return preset is null ? NotFound() : Ok(preset);
    }

    /// <summary>Creates a search preset for a vacancy platform.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SearchPresetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchPresetResponse>> Create(
        CreateSearchPresetRequest request,
        CancellationToken cancellationToken)
    {
        var preset = await presetService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = preset.Id }, preset);
    }

    /// <summary>Updates a saved search preset.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SearchPresetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SearchPresetResponse>> Update(
        Guid id,
        UpdateSearchPresetRequest request,
        CancellationToken cancellationToken)
    {
        var preset = await presetService.UpdateAsync(id, request, cancellationToken);
        return preset is null ? NotFound() : Ok(preset);
    }

    /// <summary>Deletes a saved search preset.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await presetService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }
}
