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
public class PetFollowersController : ControllerBase
{
    private readonly IPetFollowerService _followerService;
    private readonly IPetService _petService;

    public PetFollowersController(IPetFollowerService followerService, IPetService petService)
    {
        _followerService = followerService;
        _petService = petService;
    }

    [HttpGet("{petId}")]
    [ProducesResponseType<PagedResult<PetFollowerResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFollowers(
        string petId,
        [FromQuery] PetFollowerSearchFilter filter,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdministrator = User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());
        if (!await _petService.CanEditAsync(petId, userId, isAdministrator, cancellationToken: cancellationToken))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _followerService.GetFollowersAsync(petId, filter, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("{petId}/users")]
    [ProducesResponseType<PetFollowerResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FollowAsUser(string petId, CancellationToken cancellationToken)
    {
        try
        {
            var follower = await _followerService.FollowUserAsync(
                petId,
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                cancellationToken);
            return StatusCode(StatusCodes.Status201Created, follower);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (PetNotFoundException)
        {
            return NotFound(new { message = "Pet não encontrado ou não disponível para seguir." });
        }
        catch (PetFollowerNotFoundException)
        {
            return NotFound(new { message = "Usuário autenticado não encontrado ou inativo." });
        }
        catch (PetFollowerPermissionException)
        {
            return Forbid();
        }
        catch (PetAlreadyFollowedException)
        {
            return Conflict(new { message = "Este usuário já segue o pet." });
        }
    }

    [HttpDelete("{petId}/users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnfollowAsUser(string petId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return await _followerService.UnfollowUserAsync(petId, userId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpPost("{petId}/pets/{followerPetId}")]
    [ProducesResponseType<PetFollowerResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FollowAsPet(string petId, string followerPetId, CancellationToken cancellationToken)
    {
        try
        {
            var follower = await _followerService.FollowPetAsync(
                petId,
                followerPetId,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.IsInRole(AccessLevel.ADMINISTRADOR.ToString()),
                cancellationToken);
            return StatusCode(StatusCodes.Status201Created, follower);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (PetNotFoundException)
        {
            return NotFound(new { message = "Pet que será seguido não encontrado ou indisponível." });
        }
        catch (PetFollowerNotFoundException)
        {
            return NotFound(new { message = "Pet seguidor não encontrado." });
        }
        catch (PetFollowerPermissionException)
        {
            return Forbid();
        }
        catch (PetAlreadyFollowedException)
        {
            return Conflict(new { message = "Este pet já segue o pet informado." });
        }
    }

    [HttpDelete("{petId}/pets/{followerPetId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnfollowAsPet(string petId, string followerPetId, CancellationToken cancellationToken)
    {
        try
        {
            var removed = await _followerService.UnfollowPetAsync(
                petId,
                followerPetId,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.IsInRole(AccessLevel.ADMINISTRADOR.ToString()),
                cancellationToken);
            return removed ? NoContent() : NotFound();
        }
        catch (PetFollowerPermissionException)
        {
            return Forbid();
        }
    }
}