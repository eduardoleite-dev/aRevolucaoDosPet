using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace aRevolucaoDosPet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICloudinaryService _cloudinaryService;

    public UsersController(IUserService userService, ICloudinaryService cloudinaryService)
    {
        _userService = userService;
        _cloudinaryService = cloudinaryService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<UserResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.RegisterAsync(request, cancellationToken);
            if (user is null)
            {
                return Conflict(new { message = "Já existe um usuário cadastrado com este e-mail." });
            }

            return StatusCode(StatusCodes.Status201Created, user);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Search(
        [FromQuery] UserSearchFilter filter,
        CancellationToken cancellationToken)
    {
        return Forbid();
    }

    [Authorize]
    [HttpGet("{id}")]
    [ProducesResponseType<UserResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        if (!CanAccess(id))
        {
            return Forbid();
        }

        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user is null || user.Status == UserStatus.DELETED.ToString())
        {
            return NotFound();
        }

        return Ok(user);
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType<UserResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanAccess(id))
        {
            return Forbid();
        }

        try
        {
            var user = await _userService.UpdateAsync(id, request, cancellationToken);
            return user is null
                ? NotFound()
                : Ok(user);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (DuplicateUserEmailException)
        {
            return Conflict(new { message = "Já existe um usuário cadastrado com este e-mail." });
        }
    }

    [Authorize]
    [HttpPost("{id}/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(51 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 51 * 1024 * 1024)]
    [ProducesResponseType<UserResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadPhoto(
        string id,
        [FromForm] UserPhotoUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanAccess(id))
        {
            return Forbid();
        }

        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user is null || user.Status == UserStatus.DELETED.ToString())
        {
            return NotFound();
        }

        try
        {
            var upload = await _cloudinaryService.UploadAsync(
                request.File,
                $"user_{id}_avatar",
                cancellationToken);
            var updatedUser = await _userService.UpdateAvatarAsync(id, upload.Url, cancellationToken);
            return updatedUser is null ? NotFound() : Ok(updatedUser);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (!CanAccess(id))
        {
            return Forbid();
        }

        return await _userService.DeleteAsync(id, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private bool CanAccess(string id) =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) == id;
}