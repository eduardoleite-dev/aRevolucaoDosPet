using aRevolucaoDosPet.DTOs;
using Microsoft.AspNetCore.Http;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface ICloudinaryService
{
    Task<MediaUploadResponseDto> UploadAsync(IFormFile file, string name, CancellationToken cancellationToken);
}