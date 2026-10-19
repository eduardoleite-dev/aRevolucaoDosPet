using System.Security.Claims;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aRevolucaoDosPet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsAdministrator => User.IsInRole(AccessLevel.ADMINISTRADOR.ToString());

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<PagedResult<PostResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] PostSearchFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _postService.SearchAsync(filter, CurrentUserId, IsAdministrator, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType<PostResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var post = await _postService.GetByIdAsync(id, CurrentUserId, IsAdministrator, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<PostResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var post = await _postService.CreateAsync(request, CurrentUserId, IsAdministrator, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, post);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (PostPetNotFoundException)
        {
            return NotFound(new { message = "Pet não encontrado." });
        }
        catch (PostAuthorNotFoundException)
        {
            return NotFound(new { message = "Autor não encontrado ou inativo." });
        }
        catch (PostPermissionException)
        {
            return Forbid();
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType<PostResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] PostWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var post = await _postService.UpdateAsync(id, request, CurrentUserId, IsAdministrator, cancellationToken);
            return post is null ? NotFound() : Ok(post);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (PostPermissionException)
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
            return await _postService.DeleteAsync(id, CurrentUserId, IsAdministrator, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (PostPermissionException)
        {
            return Forbid();
        }
    }
}
