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
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    [HttpGet]
    [ProducesResponseType<PagedResult<AppointmentResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search([FromQuery] AppointmentSearchFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _appointmentService.SearchAsync(filter, CurrentUserId, IsAdministrator, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (RecordPetNotFoundException)
        {
            return NotFound(new { message = "Pet não encontrado." });
        }
        catch (RecordPermissionException)
        {
            return Forbid();
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType<AppointmentResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var appointment = await _appointmentService.GetByIdAsync(id, CurrentUserId, IsAdministrator, cancellationToken);
            return appointment is null ? NotFound() : Ok(appointment);
        }
        catch (RecordPermissionException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [ProducesResponseType<AppointmentResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var appointment = await _appointmentService.CreateAsync(request, CurrentUserId, IsAdministrator, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, appointment);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (RecordPetNotFoundException)
        {
            return NotFound(new { message = "Pet não encontrado." });
        }
        catch (RecordPermissionException)
        {
            return Forbid();
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType<AppointmentResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] AppointmentWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var appointment = await _appointmentService.UpdateAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken);
            return appointment is null ? NotFound() : Ok(appointment);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (RecordPermissionException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            return await _appointmentService.DeleteAsync(id, CurrentUserId, IsAdministrator, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (RecordPermissionException)
        {
            return Forbid();
        }
    }
}
