using System.Security.Claims;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aRevolucaoDosPet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiaryEntriesController : ControllerBase
{
    private readonly IDiaryEntryService _diaryService;

    public DiaryEntriesController(IDiaryEntryService diaryService)
    {
        _diaryService = diaryService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<PagedResult<DiaryEntryResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search([FromQuery] DiaryEntrySearchFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            var entries = await _diaryService.SearchAsync(filter, CurrentUserId, IsAdministrator, cancellationToken);
            return entries is null ? NotFound() : Ok(entries);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType<DiaryEntryResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var entry = await _diaryService.GetByIdAsync(id, CurrentUserId, IsAdministrator, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<DiaryEntryResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateDiaryEntryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _diaryService.CreateAsync(request, CurrentUserId, IsAdministrator, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, entry);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (RecordPetNotFoundException)
        {
            return NotFound(new { message = "Pet não encontrado." });
        }
        catch (DiaryAuthorNotFoundException)
        {
            return NotFound(new { message = "Autor não encontrado ou inativo." });
        }
        catch (RecordPermissionException)
        {
            return Forbid();
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType<DiaryEntryResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] DiaryEntryWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _diaryService.UpdateAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken);
            return entry is null ? NotFound() : Ok(entry);
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

    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            return await _diaryService.DeleteAsync(id, CurrentUserId, IsAdministrator, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (RecordPermissionException)
        {
            return Forbid();
        }
    }
}
