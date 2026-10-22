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
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    public NotificationsController(INotificationService service) => _service = service;

    [HttpGet]
    [ProducesResponseType<PagedResult<NotificationResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search([FromQuery] NotificationSearchFilter filter, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.SearchAsync(filter, CurrentUserId, IsAdministrator, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (RecordPermissionException) { return Forbid(); }
    }

    [HttpGet("{id}")]
    [ProducesResponseType<NotificationResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try { var result = await _service.GetByIdAsync(id, CurrentUserId, IsAdministrator, cancellationToken); return result is null ? NotFound() : Ok(result); }
        catch (RecordPermissionException) { return Forbid(); }
    }

    [Authorize(Roles = nameof(AccessLevel.ADMINISTRADOR))]
    [HttpPost]
    [ProducesResponseType<NotificationResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        try { return StatusCode(StatusCodes.Status201Created, await _service.CreateAsync(request, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (NotificationUserNotFoundException) { return NotFound(new { message = "Usuário destinatário não encontrado ou inativo." }); }
        catch (NotificationPetNotFoundException) { return NotFound(new { message = "Pet relacionado não encontrado." }); }
    }

    [HttpPost("{id}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(string id, CancellationToken cancellationToken)
    {
        try { return await _service.MarkReadAsync(id, CurrentUserId, IsAdministrator, cancellationToken) ? NoContent() : NotFound(); }
        catch (RecordPermissionException) { return Forbid(); }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try { return await _service.DeleteAsync(id, CurrentUserId, IsAdministrator, cancellationToken) ? NoContent() : NotFound(); }
        catch (RecordPermissionException) { return Forbid(); }
    }
}
