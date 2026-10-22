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
public class WalksController : ControllerBase
{
    private readonly IWalkService _service;
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    public WalksController(IWalkService service) => _service = service;

    [HttpGet]
    [ProducesResponseType<PagedResult<WalkResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search([FromQuery] WalkSearchFilter filter, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.SearchAsync(filter, CurrentUserId, IsAdministrator, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (WalkPetNotFoundException) { return NotFound(new { message = "Pet não encontrado." }); }
        catch (WalkPermissionException) { return Forbid(); }
    }

    [HttpGet("{id}")]
    [ProducesResponseType<WalkResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try { var item = await _service.GetByIdAsync(id, CurrentUserId, IsAdministrator, cancellationToken); return item is null ? NotFound() : Ok(item); }
        catch (WalkPermissionException) { return Forbid(); }
    }

    [HttpPost]
    [ProducesResponseType<WalkResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateWalkRequest request, CancellationToken cancellationToken)
    {
        try { return StatusCode(StatusCodes.Status201Created, await _service.CreateAsync(request, CurrentUserId, IsAdministrator, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (WalkPetNotFoundException) { return NotFound(new { message = "Pet não encontrado." }); }
        catch (WalkUserNotFoundException) { return NotFound(new { message = "Usuário que passeou não encontrado." }); }
        catch (WalkPermissionException) { return Forbid(); }
    }

    [HttpPut("{id}")]
    [ProducesResponseType<WalkResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] WalkWriteRequest request, CancellationToken cancellationToken)
    {
        try { var item = await _service.UpdateAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken); return item is null ? NotFound() : Ok(item); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
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
