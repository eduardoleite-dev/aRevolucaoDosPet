using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Services.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;

namespace aRevolucaoDosPet.Services;

public class CloudinaryService : ICloudinaryService
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;
    private const string UploadFolder = "aRevolucaoDosPet";

    private static readonly IReadOnlyDictionary<string, string> SupportedMedia = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
        [".mp4"] = "video/mp4",
        [".mov"] = "video/quicktime",
        [".webm"] = "video/webm"
    };

    private readonly Cloudinary _cloudinary;

    public CloudinaryService(Cloudinary cloudinary)
    {
        _cloudinary = cloudinary;
    }

    public async Task<MediaUploadResponseDto> UploadAsync(IFormFile file, string name, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("Selecione um arquivo válido.");
        if (file.Length > MaxFileSizeBytes)
            throw new ArgumentException("O arquivo não pode ultrapassar 50 MB.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 ||
            !name.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
            throw new ArgumentException("O nome deve conter de 1 a 100 letras, números, hífens ou sublinhados.");

        var extension = Path.GetExtension(file.FileName);
        if (!SupportedMedia.TryGetValue(extension, out var expectedContentType) ||
            !string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Formato não suportado. Envie uma imagem JPEG, PNG, WebP ou GIF, ou um vídeo MP4, MOV ou WebM.");

        await using var stream = file.OpenReadStream();
    var fileDescription = new FileDescription($"{name}{extension}", stream);

        if (expectedContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            var result = await _cloudinary.UploadAsync(new ImageUploadParams
            {
                File = fileDescription,
                Folder = UploadFolder,
                PublicId = name
            }, cancellationToken: cancellationToken);

            return new MediaUploadResponseDto(result.SecureUrl.ToString(), result.PublicId, "image");
        }

        var videoResult = await _cloudinary.UploadAsync(new VideoUploadParams
        {
            File = fileDescription,
            Folder = UploadFolder,
            PublicId = name
        }, cancellationToken: cancellationToken);

        return new MediaUploadResponseDto(videoResult.SecureUrl.ToString(), videoResult.PublicId, "video");
    }
}