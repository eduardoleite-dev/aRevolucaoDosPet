using System.ComponentModel.DataAnnotations;
using aRevolucaoDosPet.Enums;

namespace aRevolucaoDosPet.DTOs;

public class WalkWeatherDto
{
    [Range(-100, 100)]
    public double? Temperature { get; set; }

    [StringLength(80)]
    public string? Condition { get; set; }
}

public class WalkPhotoDto
{
    [Required]
    [Url]
    [StringLength(2048)]
    public string Url { get; set; } = string.Empty;

    public UserLocationRequest? Location { get; set; }
}

public class WalkWriteRequest
{
    [EnumDataType(typeof(WalkStatus))]
    public WalkStatus Status { get; set; } = WalkStatus.IN_PROGRESS;

    public required DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; set; }

    [Range(0, 604800)]
    public int? DurationSeconds { get; set; }

    [Range(0, 1000000)]
    public decimal? DistanceMeters { get; set; }

    [Range(0, 100)]
    public decimal? AverageSpeedKmh { get; set; }

    [MinLength(2)]
    [MaxLength(100000)]
    public List<double[]>? RouteCoordinates { get; set; }

    public UserLocationRequest? StartLocation { get; set; }
    public UserLocationRequest? EndLocation { get; set; }
    public WalkWeatherDto? Weather { get; set; }

    [MaxLength(50)]
    public List<WalkPhotoDto> Photos { get; set; } = [];

    [StringLength(5000)]
    public string? Notes { get; set; }
}

public class CreateWalkRequest : WalkWriteRequest
{
    [Required]
    public string PetId { get; set; } = string.Empty;

    // Só administradores podem registrar o passeio em nome de outro usuário.
    public string? UserId { get; set; }
}

public class WalkSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? PetId { get; set; }
    public string? UserId { get; set; }
    public WalkStatus? Status { get; set; }
    public DateTime? StartedAtFrom { get; set; }
    public DateTime? StartedAtTo { get; set; }
    public DateTime? EndedAtFrom { get; set; }
    public DateTime? EndedAtTo { get; set; }
    public int? MinDurationSeconds { get; set; }
    public int? MaxDurationSeconds { get; set; }
    public decimal? MinDistanceMeters { get; set; }
    public decimal? MaxDistanceMeters { get; set; }
    public decimal? MinAverageSpeedKmh { get; set; }
    public decimal? MaxAverageSpeedKmh { get; set; }
    public bool? HasRoute { get; set; }
    public bool? HasStartLocation { get; set; }
    public bool? HasEndLocation { get; set; }
    public bool? HasPhotos { get; set; }
    public string? WeatherCondition { get; set; }
    public double? MinTemperature { get; set; }
    public double? MaxTemperature { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public double? RadiusMeters { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? UpdatedAtFrom { get; set; }
    public DateTime? UpdatedAtTo { get; set; }
}

public record WalkRouteDto(string Type, IReadOnlyList<double[]> Coordinates);
public record WalkResponseDto(
    string Id,
    string PetId,
    string UserId,
    WalkStatus Status,
    DateTime StartedAt,
    DateTime? EndedAt,
    int? DurationSeconds,
    decimal? DistanceMeters,
    decimal? AverageSpeedKmh,
    WalkRouteDto? Route,
    UserLocationRequest? StartLocation,
    UserLocationRequest? EndLocation,
    WalkWeatherDto? Weather,
    IReadOnlyList<WalkPhotoDto> Photos,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public class WalkEncounterWriteRequest
{
    [Required]
    public string PetId { get; set; } = string.Empty;

    public string? OtherPetId { get; set; }

    [StringLength(120)]
    public string? OtherPetName { get; set; }

    public UserLocationRequest? Location { get; set; }
    public WalkInteractionType? InteractionType { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class WalkEncounterSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? PetId { get; set; }
    public string? OtherPetId { get; set; }
    public WalkInteractionType? InteractionType { get; set; }
    public bool? HasOtherPetId { get; set; }
    public bool? HasLocation { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public double? RadiusMeters { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
}

public record WalkEncounterResponseDto(
    string Id,
    string WalkId,
    string PetId,
    string? OtherPetId,
    string? OtherPetName,
    UserLocationRequest? Location,
    WalkInteractionType? InteractionType,
    string? Notes,
    DateTime CreatedAt
);
