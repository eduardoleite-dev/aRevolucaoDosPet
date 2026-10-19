using System.ComponentModel.DataAnnotations;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;

namespace aRevolucaoDosPet.DTOs;

public class PetWriteRequest
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [RegularExpression("^@?[a-zA-Z0-9._]{3,30}$")]
    public string? Username { get; set; }

    [StringLength(5000)]
    public string? Bio { get; set; }

    [Url]
    [StringLength(2048)]
    public string? AvatarUrl { get; set; }

    [Url]
    [StringLength(2048)]
    public string? CoverUrl { get; set; }

    [EnumDataType(typeof(Species))]
    public required Species Species { get; init; }

    [StringLength(120)]
    public string? Breed { get; set; }

    [EnumDataType(typeof(Sex))]
    public Sex Sex { get; set; } = Sex.UNKNOWN;

    [StringLength(100)]
    public string? Color { get; set; }

    public DateTime? BirthDate { get; set; }
    public DateTime? AdoptionDate { get; set; }

    [Range(0.01, 1000)]
    public decimal? WeightKg { get; set; }

    [EnumDataType(typeof(PetSize))]
    public PetSize? Size { get; set; }

    [StringLength(100)]
    public string? Microchip { get; set; }

    [StringLength(150)]
    public string? Pedigree { get; set; }

    public bool IsNeutered { get; set; }
    public bool IsBreeder { get; set; }
    public List<string> Allergies { get; set; } = [];
    public List<string> ChronicConditions { get; set; } = [];
    public List<PetMedicationDto> ContinuousMedications { get; set; } = [];

    [StringLength(150)]
    public string? VetName { get; set; }

    [StringLength(30)]
    public string? VetPhone { get; set; }

    [StringLength(150)]
    public string? EmergencyContactName { get; set; }

    [StringLength(30)]
    public string? EmergencyContactPhone { get; set; }

    [StringLength(150)]
    public string? HealthPlan { get; set; }

    [StringLength(100)]
    public string? HealthPlanNumber { get; set; }

    [EnumDataType(typeof(PetStatus))]
    public PetStatus Status { get; set; } = PetStatus.ALIVE;

    [EnumDataType(typeof(PetProfileVisibility))]
    public PetProfileVisibility Visibility { get; set; } = PetProfileVisibility.PUBLIC;

    public UserLocationRequest? Location { get; set; }

    public List<string>? CoTutorIds { get; set; }
}

public class CreatePetRequest : PetWriteRequest
{
    public string? OwnerId { get; set; }
}

public class PetMedicationDto
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Dosage { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Frequency { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }
}

public class PetSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? OwnerId { get; set; }
    public string? CoTutorId { get; set; }
    public string? Name { get; set; }
    public string? Username { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverUrl { get; set; }
    public Species? Species { get; set; }
    public string? Breed { get; set; }
    public Sex? Sex { get; set; }
    public string? Color { get; set; }
    public DateTime? BirthDateFrom { get; set; }
    public DateTime? BirthDateTo { get; set; }
    public DateTime? AdoptionDateFrom { get; set; }
    public DateTime? AdoptionDateTo { get; set; }

    [Range(0, 1000)]
    public decimal? MinWeightKg { get; set; }

    [Range(0, 1000)]
    public decimal? MaxWeightKg { get; set; }

    public PetSize? Size { get; set; }
    public string? Microchip { get; set; }
    public string? Pedigree { get; set; }
    public bool? IsNeutered { get; set; }
    public bool? IsBreeder { get; set; }
    public string? Allergy { get; set; }
    public string? ChronicCondition { get; set; }
    public string? MedicationName { get; set; }
    public string? VetName { get; set; }
    public string? VetPhone { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? HealthPlan { get; set; }
    public string? HealthPlanNumber { get; set; }
    public PetStatus? Status { get; set; }
    public PetProfileVisibility? Visibility { get; set; }
    public bool? HasLocation { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(0.01, 20_000_000)]
    public double? RadiusMeters { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinFollowersCount { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxFollowersCount { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinFollowingCount { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxFollowingCount { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinPostsCount { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxPostsCount { get; set; }

    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? UpdatedAtFrom { get; set; }
    public DateTime? UpdatedAtTo { get; set; }
    public DateTime? DeletedAtFrom { get; set; }
    public DateTime? DeletedAtTo { get; set; }
    public bool IncludeDeleted { get; set; }
}

public record PetResponseDto(
    string Id,
    string OwnerId,
    IReadOnlyList<string> CoTutorIds,
    string Name,
    string? Username,
    string? Bio,
    string? AvatarUrl,
    string? CoverUrl,
    Species Species,
    string? Breed,
    Sex Sex,
    string? Color,
    DateTime? BirthDate,
    DateTime? AdoptionDate,
    decimal? WeightKg,
    PetSize? Size,
    string? Microchip,
    string? Pedigree,
    bool IsNeutered,
    bool IsBreeder,
    IReadOnlyList<string> Allergies,
    IReadOnlyList<string> ChronicConditions,
    IReadOnlyList<PetMedicationDto> ContinuousMedications,
    string? VetName,
    string? VetPhone,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? HealthPlan,
    string? HealthPlanNumber,
    PetStatus Status,
    PetProfileVisibility Visibility,
    UserLocation? Location,
    int FollowersCount,
    int FollowingCount,
    int PostsCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);

public record PetPublicProfileDto(
    string Id,
    string Name,
    string? Username,
    string? Bio,
    string? AvatarUrl,
    string? CoverUrl,
    Species Species,
    string? Breed,
    Sex Sex,
    string? Color,
    PetStatus Status,
    PetProfileVisibility Visibility,
    int FollowersCount,
    int PostsCount,
    DateTime CreatedAt
);

public class PetFollowerSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? FollowerUserId { get; set; }
    public string? FollowerPetId { get; set; }
}

public record PetFollowerResponseDto(
    string Id,
    string PetId,
    string? FollowerUserId,
    string? FollowerPetId,
    DateTime CreatedAt
);