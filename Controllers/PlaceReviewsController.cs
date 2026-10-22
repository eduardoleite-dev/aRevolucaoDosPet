using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aRevolucaoDosPet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlaceReviewsController : ControllerBase
{
    private readonly IPlaceReviewService _service;
    public PlaceReviewsController(IPlaceReviewService service) => _service = service;

    [AllowAnonymous]
    [HttpGet("place/{placeId}")]
    [ProducesResponseType<PagedResult<PlaceReviewResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search(string placeId, [FromQuery] PlaceReviewSearchFilter filter, CancellationToken cancellationToken)
    {
        var result = await _service.SearchAsync(placeId, filter, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType<PlaceReviewResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpPut("place/{placeId}")]
    [ProducesResponseType<PlaceReviewResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<PlaceReviewResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upsert(string placeId, [FromBody] PlaceReviewWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.UpsertAsync(placeId, request, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, cancellationToken);
            return StatusCode(StatusCodes.Status200OK, result);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (PlacePetNotFoundException) { return NotFound(new { message = "Local não encontrado." }); }
        catch (PlaceReviewUserNotFoundException) { return NotFound(new { message = "Usuário autenticado não encontrado." }); }
        catch (PlaceReviewPermissionException) { return Forbid(); }
        catch (WalkPetNotFoundException) { return NotFound(new { message = "Pet não encontrado." }); }
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType<PlaceReviewResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] PlaceReviewWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                User.IsInRole(nameof(Enums.AccessLevel.ADMINISTRADOR)), cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (PlaceReviewPermissionException) { return Forbid(); }
        catch (WalkPetNotFoundException) { return NotFound(new { message = "Pet não encontrado." }); }
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
            return await _service.DeleteAsync(id, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                User.IsInRole(nameof(Enums.AccessLevel.ADMINISTRADOR)), cancellationToken) ? NoContent() : NotFound();
        }
        catch (PlaceReviewPermissionException) { return Forbid(); }
    }
}
