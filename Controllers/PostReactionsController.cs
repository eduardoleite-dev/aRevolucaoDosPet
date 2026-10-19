using System.Security.Claims;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aRevolucaoDosPet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostReactionsController : ControllerBase
{
    private readonly IPostReactionService _reactionService;

    public PostReactionsController(IPostReactionService reactionService)
    {
        _reactionService = reactionService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    [AllowAnonymous]
    [HttpGet("{postId}")]
    [ProducesResponseType<PagedResult<PostReactionResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReactions(
        string postId,
        [FromQuery] PostReactionSearchFilter filter,
        CancellationToken cancellationToken)
    {
        try
        {
            var reactions = await _reactionService.GetReactionsAsync(
                postId,
                filter,
                CurrentUserId,
                IsAdministrator,
                cancellationToken);
            return reactions is null ? NotFound() : Ok(reactions);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpPost("{postId}")]
    [ProducesResponseType<PostReactionResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<PostReactionResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> React(
        string postId,
        [FromBody] ReactToPostRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _reactionService.ReactAsync(
                postId,
                request,
                CurrentUserId,
                IsAdministrator,
                cancellationToken);
            return result.Created
                ? StatusCode(StatusCodes.Status201Created, result.Reaction)
                : Ok(result.Reaction);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (PostNotAvailableException)
        {
            return NotFound(new { message = "Post não encontrado." });
        }
        catch (ReactionPetNotFoundException)
        {
            return NotFound(new { message = "Pet não encontrado." });
        }
        catch (ReactionPermissionException)
        {
            return Forbid();
        }
    }

    [Authorize]
    [HttpDelete("{postId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveReaction(
        string postId,
        [FromQuery] string? petId,
        CancellationToken cancellationToken)
    {
        return await _reactionService.RemoveReactionAsync(postId, petId, CurrentUserId, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
