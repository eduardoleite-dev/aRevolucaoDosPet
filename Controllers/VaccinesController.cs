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
public class VaccinesController : ControllerBase
{
    private readonly IVaccineService _vaccineService;

    public VaccinesController(IVaccineService vaccineService)
    {
        _vaccineService = vaccineService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    [HttpGet]
    [ProducesResponseType<PagedResult<VaccineResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search([FromQuery] VaccineSearchFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _vaccineService.SearchAsync(filter, CurrentUserId, IsAdministrator, cancellationToken));
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
    [ProducesResponseType<VaccineResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var vaccine = await _vaccineService.GetByIdAsync(id, CurrentUserId, IsAdministrator, cancellationToken);
            return vaccine is null ? NotFound() : Ok(vaccine);
        }
        catch (RecordPermissionException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [ProducesResponseType<VaccineResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateVaccineRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var vaccine = await _vaccineService.CreateAsync(request, CurrentUserId, IsAdministrator, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, vaccine);
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
    [ProducesResponseType<VaccineResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] VaccineWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var vaccine = await _vaccineService.UpdateAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken);
            return vaccine is null ? NotFound() : Ok(vaccine);
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
            return await _vaccineService.DeleteAsync(id, CurrentUserId, IsAdministrator, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (RecordPermissionException)
        {
            return Forbid();
        }
    }
}
