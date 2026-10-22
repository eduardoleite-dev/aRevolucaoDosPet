using System.Security.Claims;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aRevolucaoDosPet.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WalkEncountersController : ControllerBase
{
    private readonly IWalkEncounterService _service;
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    public WalkEncountersController(IWalkEncounterService service) => _service = service;

    [HttpGet("walk/{walkId}")]
    [ProducesResponseType<PagedResult<WalkEncounterResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search(string walkId, [FromQuery] WalkEncounterSearchFilter filter, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.SearchAsync(walkId, filter, CurrentUserId, IsAdministrator, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (WalkEncounterNotFoundException) { return NotFound(new { message = "Caminhada não encontrada." }); }
        catch (WalkPermissionException) { return Forbid(); }
    }

    [HttpGet("{id}")]
    [ProducesResponseType<WalkEncounterResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try { var item = await _service.GetByIdAsync(id, CurrentUserId, IsAdministrator, cancellationToken); return item is null ? NotFound() : Ok(item); }
        catch (WalkPermissionException) { return Forbid(); }
    }

    [HttpPost("walk/{walkId}")]
    [ProducesResponseType<WalkEncounterResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(string walkId, [FromBody] WalkEncounterWriteRequest request, CancellationToken cancellationToken)
    {
        try { return StatusCode(StatusCodes.Status201Created, await _service.CreateAsync(walkId, request, CurrentUserId, IsAdministrator, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (WalkEncounterNotFoundException) { return NotFound(new { message = "Caminhada não encontrada." }); }
        catch (WalkPetNotFoundException) { return NotFound(new { message = "Pet encontrado não existe." }); }
        catch (WalkPermissionException) { return Forbid(); }
    }

    [HttpPut("{id}")]
    [ProducesResponseType<WalkEncounterResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] WalkEncounterWriteRequest request, CancellationToken cancellationToken)
    {
        try { var item = await _service.UpdateAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken); return item is null ? NotFound() : Ok(item); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (WalkEncounterNotFoundException) { return NotFound(new { message = "Caminhada não encontrada." }); }
        catch (WalkPetNotFoundException) { return NotFound(new { message = "Pet encontrado não existe." }); }
        catch (WalkPermissionException) { return Forbid(); }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try { return await _service.DeleteAsync(id, CurrentUserId, IsAdministrator, cancellationToken) ? NoContent() : NotFound(); }
        catch (WalkPermissionException) { return Forbid(); }
    }
}
