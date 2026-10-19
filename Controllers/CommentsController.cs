using System.Security.Claims;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aRevolucaoDosPet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<PagedResult<CommentResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search([FromQuery] CommentSearchFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            var comments = await _commentService.SearchAsync(filter, CurrentUserId, IsAdministrator, cancellationToken);
            return comments is null ? NotFound() : Ok(comments);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType<CommentResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var comment = await _commentService.GetByIdAsync(id, CurrentUserId, IsAdministrator, cancellationToken);
        return comment is null ? NotFound() : Ok(comment);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<CommentResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var comment = await _commentService.CreateAsync(request, CurrentUserId, IsAdministrator, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, comment);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (CommentPostNotFoundException)
        {
            return NotFound(new { message = "Post não encontrado." });
        }
        catch (CommentPetNotFoundException)
        {
            return NotFound(new { message = "Pet não encontrado." });
        }
        catch (CommentParentNotFoundException)
        {
            return NotFound(new { message = "Comentário pai não encontrado neste post." });
        }
        catch (CommentPermissionException)
        {
            return Forbid();
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType<CommentResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] CommentWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var comment = await _commentService.UpdateAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken);
            return comment is null ? NotFound() : Ok(comment);
        }
        catch (CommentPermissionException)
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
            return await _commentService.DeleteAsync(id, CurrentUserId, IsAdministrator, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (CommentPermissionException)
        {
            return Forbid();
        }
    }

    [Authorize]
    [HttpPost("{id}/reactions")]
    [ProducesResponseType<CommentResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> React(string id, [FromBody] ReactToCommentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var comment = await _commentService.ReactAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken);
            return comment is null ? NotFound() : Ok(comment);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id}/reactions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveReaction(string id, CancellationToken cancellationToken)
    {
        return await _commentService.RemoveReactionAsync(id, CurrentUserId, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
