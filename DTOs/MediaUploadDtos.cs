using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace aRevolucaoDosPet.DTOs;

public class MediaUploadRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression("^[a-zA-Z0-9_-]+$", ErrorMessage = "O nome deve conter apenas letras, números, hífen ou sublinhado.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    public IFormFile File { get; set; } = null!;
}

public class UserPhotoUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
}

public record MediaUploadResponseDto(
    string Url,
    string PublicId,
    string Type
);