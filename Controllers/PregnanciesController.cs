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
public class PregnanciesController : ControllerBase
{
    private readonly IPregnancyService _service;
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    public PregnanciesController(IPregnancyService service) => _service = service;

    [HttpGet]
    [ProducesResponseType<PagedResult<PregnancyResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search([FromQuery] PregnancySearchFilter filter, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.SearchAsync(filter, CurrentUserId, IsAdministrator, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (RecordPetNotFoundException) { return NotFound(new { message = "Pet não encontrado." }); }
        catch (RecordPermissionException) { return Forbid(); }
    }

    [HttpGet("{id}")]
    [ProducesResponseType<PregnancyResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try { var item = await _service.GetByIdAsync(id, CurrentUserId, IsAdministrator, cancellationToken); return item is null ? NotFound() : Ok(item); }
        catch (RecordPermissionException) { return Forbid(); }
    }

    [HttpPost]
    [ProducesResponseType<PregnancyResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreatePregnancyRequest request, CancellationToken cancellationToken)
    {
        try { return StatusCode(StatusCodes.Status201Created, await _service.CreateAsync(request, CurrentUserId, IsAdministrator, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (RecordPermissionException) { return Forbid(); }
        catch (PregnancyPetNotFoundException) { return NotFound(new { message = "Pet mãe ou pai não encontrado." }); }
        catch (PregnancyPetSexException) { return BadRequest(new { message = "Gestação só pode ser associada a pet fêmea." }); }
    }

    [HttpPut("{id}")]
    [ProducesResponseType<PregnancyResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] PregnancyWriteRequest request, CancellationToken cancellationToken)
    {
        try { var item = await _service.UpdateAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken); return item is null ? NotFound() : Ok(item); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (RecordPermissionException) { return Forbid(); }
        catch (PregnancyPetNotFoundException) { return NotFound(new { message = "Pet mãe ou pai não encontrado." }); }
        catch (PregnancyPetSexException) { return BadRequest(new { message = "Gestação só pode ser associada a pet fêmea." }); }
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
