using System.Security.Claims;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aRevolucaoDosPet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlacesController : ControllerBase
{
    private readonly IPlaceService _service;
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    public PlacesController(IPlaceService service) => _service = service;

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<PagedResult<PlaceResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] PlaceSearchFilter filter, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.SearchAsync(filter, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType<PlaceResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var place = await _service.GetByIdAsync(id, cancellationToken);
        return place is null ? NotFound() : Ok(place);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<PlaceResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] PlaceWriteRequest request, CancellationToken cancellationToken)
    {
        try { return StatusCode(StatusCodes.Status201Created, await _service.CreateAsync(request, CurrentUserId, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType<PlaceResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] PlaceWriteRequest request, CancellationToken cancellationToken)
    {
        try { var place = await _service.UpdateAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken); return place is null ? NotFound() : Ok(place); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (RecordPermissionException) { return Forbid(); }
    }

    [Authorize]
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
