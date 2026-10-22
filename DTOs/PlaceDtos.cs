using System.ComponentModel.DataAnnotations;
using aRevolucaoDosPet.Enums;

namespace aRevolucaoDosPet.DTOs;

public class PlaceWriteRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [EnumDataType(typeof(PlaceType))]
    public required PlaceType Type { get; init; }

    [StringLength(300)]
    public string? Address { get; set; }

    [Required]
    public UserLocationRequest? Location { get; set; }

    public bool IsPetFriendly { get; set; } = true;
    public bool HasWater { get; set; }
    public bool HasShade { get; set; }
    public bool IsFenced { get; set; }

    [MaxLength(30)]
    public List<string> Hazards { get; set; } = [];
}

public class PlaceSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public PlaceType? Type { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public bool? IsPetFriendly { get; set; }
    public bool? HasWater { get; set; }
    public bool? HasShade { get; set; }
    public bool? IsFenced { get; set; }
    public string? Hazard { get; set; }
    public double? MinRating { get; set; }
    public double? MaxRating { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public double? RadiusMeters { get; set; }
}

public record PlaceResponseDto(
    string Id,
    string Name,
    PlaceType Type,
    string? Address,
    UserLocationRequest Location,
    bool IsPetFriendly,
    bool HasWater,
    bool HasShade,
    bool IsFenced,
    decimal AverageRating,
    int ReviewsCount,
    IReadOnlyList<string> Hazards,
    string? CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public class PlaceReviewPhotoDto
{
    [Required]
    [Url]
    [StringLength(2048)]
    public string Url { get; set; } = string.Empty;

    [StringLength(255)]
    public string? PublicId { get; set; }
}

public class PlaceReviewWriteRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(3000)]
    public string? Comment { get; set; }

    [MaxLength(10)]
    public List<PlaceReviewPhotoDto> Photos { get; set; } = [];

    public string? PetId { get; set; }
}

public class PlaceReviewSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public int? Rating { get; set; }
    public string? UserId { get; set; }
    public string? PetId { get; set; }
    public bool? HasComment { get; set; }
    public bool? HasPhotos { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
}

public record PlaceReviewResponseDto(
    string Id,
    string PlaceId,
    string UserId,
    string? PetId,
    int Rating,
    string? Comment,
    IReadOnlyList<PlaceReviewPhotoDto> Photos,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
