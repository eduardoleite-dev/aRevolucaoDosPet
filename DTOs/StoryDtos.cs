using System.ComponentModel.DataAnnotations;

namespace aRevolucaoDosPet.DTOs;

public class CreateStoryRequest
{
    [Required]
    public string PetId { get; set; } = string.Empty;

    // Só administradores podem publicar em nome de outro usuário.
    public string? AuthorUserId { get; set; }

    [Required]
    [Url]
    [StringLength(2048)]
    public string MediaUrl { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(image|video)$", ErrorMessage = "O tipo da mídia deve ser image ou video.")]
    public string MediaType { get; set; } = "image";

    [StringLength(2200)]
    public string? Caption { get; set; }
}

public class UpdateStoryRequest
{
    [StringLength(2200)]
    public string? Caption { get; set; }
}

public class StorySearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? PetId { get; set; }
    public string? AuthorUserId { get; set; }
    public string? MediaType { get; set; }
    public string? Caption { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinViewsCount { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxViewsCount { get; set; }

    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? ExpiresAtFrom { get; set; }
    public DateTime? ExpiresAtTo { get; set; }
    public bool IncludeExpired { get; set; }
}

public record StoryResponseDto(
    string Id,
    string PetId,
    string AuthorUserId,
    string MediaUrl,
    string MediaType,
    string? Caption,
    int ViewsCount,
    DateTime ExpiresAt,
    DateTime CreatedAt
);
