using System.Security.Claims;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aRevolucaoDosPet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoriesController : ControllerBase
{
    private readonly IStoryService _storyService;

    public StoriesController(IStoryService storyService)
    {
        _storyService = storyService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<PagedResult<StoryResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search([FromQuery] StorySearchFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            var stories = await _storyService.SearchAsync(filter, CurrentUserId, IsAdministrator, cancellationToken);
            return stories is null ? NotFound() : Ok(stories);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType<StoryResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var story = await _storyService.GetByIdAsync(id, CurrentUserId, IsAdministrator, cancellationToken);
        return story is null ? NotFound() : Ok(story);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<StoryResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateStoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var story = await _storyService.CreateAsync(request, CurrentUserId, IsAdministrator, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, story);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (StoryPetNotFoundException)
        {
            return NotFound(new { message = "Pet não encontrado." });
        }
        catch (StoryAuthorNotFoundException)
        {
            return NotFound(new { message = "Autor não encontrado ou inativo." });
        }
        catch (StoryPermissionException)
        {
            return Forbid();
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType<StoryResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateStoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var story = await _storyService.UpdateAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken);
            return story is null ? NotFound() : Ok(story);
        }
        catch (StoryPermissionException)
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
            return await _storyService.DeleteAsync(id, CurrentUserId, IsAdministrator, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (StoryPermissionException)
        {
            return Forbid();
        }
    }

    [Authorize]
    [HttpPost("{id}/views")]
    [ProducesResponseType<StoryResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterView(string id, CancellationToken cancellationToken)
    {
        var story = await _storyService.RegisterViewAsync(id, CurrentUserId, IsAdministrator, cancellationToken);
        return story is null ? NotFound() : Ok(story);
    }
}
