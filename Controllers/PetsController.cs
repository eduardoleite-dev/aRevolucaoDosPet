using System.Security.Claims;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aRevolucaoDosPet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController : ControllerBase
{
    private readonly IPetService _petService;

    public PetsController(IPetService petService)
    {
        _petService = petService;
    }

    [Authorize(Roles = nameof(AccessLevel.ADMINISTRADOR))]
    [HttpGet]
    [ProducesResponseType<PagedResult<PetResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] PetSearchFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _petService.SearchAsync(filter, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType<PetPublicProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<PetResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var viewerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdministrator = User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());
        var pet = await _petService.GetByIdAsync(id, viewerUserId, isAdministrator, cancellationToken);
        if (pet is null)
        {
            return NotFound();
        }

        var canViewPrivateDetails = isAdministrator ||
                                    pet.OwnerId == viewerUserId ||
                                    pet.CoTutorIds.Contains(viewerUserId ?? string.Empty);
        if (canViewPrivateDetails)
        {
            return Ok(pet);
        }

        return Ok(new PetPublicProfileDto(
            pet.Id,
            pet.Name,
            pet.Username,
            pet.Bio,
            pet.AvatarUrl,
            pet.CoverUrl,
            pet.Species,
            pet.Breed,
            pet.Sex,
            pet.Color,
            pet.Status,
            pet.Visibility,
            pet.FollowersCount,
            pet.PostsCount,
            pet.CreatedAt));
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<PetResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreatePetRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pet = await _petService.CreateAsync(
                request,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.IsInRole(AccessLevel.ADMINISTRADOR.ToString()),
                cancellationToken);
            return StatusCode(StatusCodes.Status201Created, pet);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (PetOwnerNotFoundException)
        {
            return NotFound(new { message = "O tutor principal não existe ou está inativo." });
        }
        catch (DuplicatePetUsernameException)
        {
            return Conflict(new { message = "Este username já está em uso." });
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType<PetResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(string id, [FromBody] PetWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pet = await _petService.UpdateAsync(
                id,
                request,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.IsInRole(AccessLevel.ADMINISTRADOR.ToString()),
                cancellationToken);
            return pet is null ? NotFound() : Ok(pet);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (PetPermissionException)
        {
            return Forbid();
        }
        catch (DuplicatePetUsernameException)
        {
            return Conflict(new { message = "Este username já está em uso." });
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
            var deleted = await _petService.DeleteAsync(
                id,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.IsInRole(AccessLevel.ADMINISTRADOR.ToString()),
                cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (PetPermissionException)
        {
            return Forbid();
        }
    }
}