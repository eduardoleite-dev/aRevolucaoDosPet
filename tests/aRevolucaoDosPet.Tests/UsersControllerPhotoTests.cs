using System.Security.Claims;
using aRevolucaoDosPet.Controllers;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace aRevolucaoDosPet.Tests;

public class UsersControllerPhotoTests
{
    private const string UserId = "507f1f77bcf86cd799439011";
    private const string PhotoUrl = "https://res.cloudinary.com/demo/image/upload/avatar.jpg";

    [Fact]
    public async Task UploadPhoto_UploadsAndSavesAvatarUrl()
    {
        var users = new StubUserService(CreateUser(UserId, null));
        var cloudinary = new StubCloudinaryService(new MediaUploadResponseDto(PhotoUrl, "user_avatar", "image"));
        var controller = CreateController(users, cloudinary, UserId);
        using var stream = new MemoryStream([1]);
        var file = new FormFile(stream, 0, stream.Length, "file", "avatar.jpg");

        var result = await controller.UploadPhoto(
            UserId,
            new UserPhotoUploadRequest { File = file },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.Equal(PhotoUrl, response.AvatarUrl);
        Assert.Same(file, cloudinary.UploadedFile);
        Assert.Equal($"user_{UserId}_avatar", cloudinary.UploadedName);
        Assert.Equal(PhotoUrl, users.UpdatedAvatarUrl);
    }

    [Fact]
    public async Task UploadPhoto_ForDifferentUser_ReturnsForbidWithoutUploading()
    {
        var users = new StubUserService(CreateUser(UserId, null));
        var cloudinary = new StubCloudinaryService(new MediaUploadResponseDto(PhotoUrl, "user_avatar", "image"));
        var controller = CreateController(users, cloudinary, "507f1f77bcf86cd799439012");

        var result = await controller.UploadPhoto(
            UserId,
            new UserPhotoUploadRequest(),
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        Assert.Null(cloudinary.UploadedFile);
        Assert.Null(users.UpdatedAvatarUrl);
    }

    private static UsersController CreateController(StubUserService users, StubCloudinaryService cloudinary, string principalUserId)
    {
        var controller = new UsersController(users, cloudinary)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, principalUserId)], "test"))
                }
            }
        };

        return controller;
    }

    private static UserResponseDto CreateUser(string id, string? avatarUrl) => new(
        id,
        "Test User",
        "test@example.com",
        avatarUrl,
        null,
        null,
        null,
        null,
        "BR",
        null,
        "ACTIVE",
        null,
        null,
        DateTime.UtcNow,
        DateTime.UtcNow,
        null);

    private sealed class StubCloudinaryService(MediaUploadResponseDto response) : ICloudinaryService
    {
        public IFormFile? UploadedFile { get; private set; }
        public string? UploadedName { get; private set; }

        public Task<MediaUploadResponseDto> UploadAsync(IFormFile file, string name, CancellationToken cancellationToken)
        {
            UploadedFile = file;
            UploadedName = name;
            return Task.FromResult(response);
        }
    }

    private sealed class StubUserService(UserResponseDto user) : IUserService
    {
        public string? UpdatedAvatarUrl { get; private set; }

        public Task<UserResponseDto?> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserResponseDto?>(null);

        public Task<PagedResult<UserResponseDto>> SearchAsync(UserSearchFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<UserResponseDto>([], 0, 1, 20));

        public Task<UserResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserResponseDto?>(id == user.Id ? user : null);

        public Task<UserResponseDto?> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserResponseDto?>(null);

        public Task<UserResponseDto?> UpdateAvatarAsync(string id, string avatarUrl, CancellationToken cancellationToken = default)
        {
            UpdatedAvatarUrl = avatarUrl;
            return Task.FromResult<UserResponseDto?>(user with { AvatarUrl = avatarUrl });
        }

        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}