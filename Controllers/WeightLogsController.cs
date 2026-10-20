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
public class WeightLogsController : ControllerBase
{
    private readonly IWeightLogService _weightLogService;

    public WeightLogsController(IWeightLogService weightLogService)
    {
        _weightLogService = weightLogService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    [HttpGet]
    [ProducesResponseType<PagedResult<WeightLogResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search([FromQuery] WeightLogSearchFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _weightLogService.SearchAsync(filter, CurrentUserId, IsAdministrator, cancellationToken));
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
    [ProducesResponseType<WeightLogResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var weightLog = await _weightLogService.GetByIdAsync(id, CurrentUserId, IsAdministrator, cancellationToken);
            return weightLog is null ? NotFound() : Ok(weightLog);
        }
        catch (RecordPermissionException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [ProducesResponseType<WeightLogResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateWeightLogRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var weightLog = await _weightLogService.CreateAsync(request, CurrentUserId, IsAdministrator, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, weightLog);
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
    [ProducesResponseType<WeightLogResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] WeightLogWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var weightLog = await _weightLogService.UpdateAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken);
            return weightLog is null ? NotFound() : Ok(weightLog);
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
            return await _weightLogService.DeleteAsync(id, CurrentUserId, IsAdministrator, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (RecordPermissionException)
        {
            return Forbid();
        }
    }
}
