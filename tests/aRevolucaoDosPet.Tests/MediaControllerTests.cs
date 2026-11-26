using aRevolucaoDosPet.Controllers;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace aRevolucaoDosPet.Tests;

public class MediaControllerTests
{
    [Fact]
    public void Upload_DoesNotAllowAnonymousRequests()
    {
        var method = typeof(MediaController).GetMethod(nameof(MediaController.Upload));

        Assert.NotNull(method);
        Assert.Null(Attribute.GetCustomAttribute(method, typeof(AllowAnonymousAttribute)));
    }

    [Fact]
    public async Task Upload_ReturnsUrlAndPublicId()
    {
        var expected = new MediaUploadResponseDto(
            "https://res.cloudinary.com/demo/image/upload/photo.jpg",
            "aRevolucaoDosPet/photo",
            "image");
        var service = new StubCloudinaryService(expected);
        var controller = new MediaController(service);
        using var stream = new MemoryStream([1]);
        var file = new FormFile(stream, 0, stream.Length, "file", "photo.jpg");

        var result = await controller.Upload(
            new MediaUploadRequest { File = file, Name = "photo_profile" },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, okResult.Value);
        Assert.Same(file, service.UploadedFile);
        Assert.Equal("photo_profile", service.UploadedName);
    }

    [Fact]
    public async Task Upload_MapsArgumentExceptionToBadRequest()
    {
        var service = new StubCloudinaryService(new ArgumentException("Formato não suportado."));
        var controller = new MediaController(service);

        var result = await controller.Upload(
            new MediaUploadRequest { Name = "photo_profile" },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var message = badRequest.Value!.GetType().GetProperty("message")!.GetValue(badRequest.Value);
        Assert.Equal("Formato não suportado.", message);
    }

    private sealed class StubCloudinaryService : ICloudinaryService
    {
        private readonly MediaUploadResponseDto? _response;
        private readonly Exception? _exception;

        public StubCloudinaryService(MediaUploadResponseDto response)
        {
            _response = response;
        }

        public StubCloudinaryService(Exception exception)
        {
            _exception = exception;
        }

        public IFormFile? UploadedFile { get; private set; }
        public string? UploadedName { get; private set; }

        public Task<MediaUploadResponseDto> UploadAsync(IFormFile file, string name, CancellationToken cancellationToken)
        {
            UploadedFile = file;
            UploadedName = name;
            if (_exception is not null)
                throw _exception;

            return Task.FromResult(_response!);
        }
    }
}
