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
public class UserSettingsController : ControllerBase
{
    private readonly IUserSettingsService _settingsService;

    public UserSettingsController(IUserSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpPost("{userId}")]
    [ProducesResponseType<UserSettingsResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        string userId,
        [FromBody] UserSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanAccess(userId))
        {
            return Forbid();
        }

        try
        {
            var settings = await _settingsService.CreateAsync(userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, settings);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (SettingsUserNotFoundException)
        {
            return NotFound(new { message = "Usuário não encontrado ou excluído." });
        }
        catch (UserSettingsAlreadyExistsException)
        {
            return Conflict(new { message = "Este usuário já possui configurações." });
        }
    }

    [HttpGet("{userId}")]
    [ProducesResponseType<UserSettingsResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken cancellationToken)
    {
        if (!CanAccess(userId))
        {
            return Forbid();
        }

        var settings = await _settingsService.GetByUserIdAsync(userId, cancellationToken);
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpPut("{userId}")]
    [ProducesResponseType<UserSettingsResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string userId,
        [FromBody] UserSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanAccess(userId))
        {
            return Forbid();
        }

        var settings = await _settingsService.UpdateAsync(userId, request, cancellationToken);
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string userId, CancellationToken cancellationToken)
    {
        if (!CanAccess(userId))
        {
            return Forbid();
        }

        return await _settingsService.DeleteAsync(userId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private bool CanAccess(string userId) =>
        User.IsInRole(AccessLevel.ADMINISTRADOR.ToString()) ||
        User.FindFirstValue(ClaimTypes.NameIdentifier) == userId;
}