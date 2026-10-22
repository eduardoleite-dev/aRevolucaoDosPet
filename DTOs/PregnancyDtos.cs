using System.ComponentModel.DataAnnotations;
using aRevolucaoDosPet.Enums;

namespace aRevolucaoDosPet.DTOs;

public class PregnancyWriteRequest
{
    public string? FatherPetId { get; set; }

    [StringLength(150)]
    public string? FatherName { get; set; }

    public required DateTime StartedAt { get; init; }
    public DateTime? ExpectedBirthAt { get; set; }
    public DateTime? ActualBirthAt { get; set; }

    [EnumDataType(typeof(PregnancyStatus))]
    public PregnancyStatus Status { get; set; } = PregnancyStatus.ONGOING;

    [Url]
    [StringLength(2048)]
    public string? UltrasoundUrl { get; set; }

    [Range(0, 30)]
    public int? PuppiesCount { get; set; }

    [StringLength(5000)]
    public string? Notes { get; set; }
}

public class CreatePregnancyRequest : PregnancyWriteRequest
{
    [Required]
    public string PetId { get; set; } = string.Empty;
}

public class PregnancySearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? PetId { get; set; }
    public string? FatherPetId { get; set; }
    public string? FatherName { get; set; }
    public PregnancyStatus? Status { get; set; }
    public DateTime? StartedAtFrom { get; set; }
    public DateTime? StartedAtTo { get; set; }
    public DateTime? ExpectedBirthAtFrom { get; set; }
    public DateTime? ExpectedBirthAtTo { get; set; }
    public DateTime? ActualBirthAtFrom { get; set; }
    public DateTime? ActualBirthAtTo { get; set; }
    public bool? HasUltrasound { get; set; }

    [Range(0, 30)]
    public int? MinPuppiesCount { get; set; }

    [Range(0, 30)]
    public int? MaxPuppiesCount { get; set; }

    public string? Notes { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? UpdatedAtFrom { get; set; }
    public DateTime? UpdatedAtTo { get; set; }
}

public record PregnancyResponseDto(
    string Id,
    string PetId,
    string? FatherPetId,
    string? FatherName,
    DateTime StartedAt,
    DateTime? ExpectedBirthAt,
    DateTime? ActualBirthAt,
    PregnancyStatus Status,
    string? UltrasoundUrl,
    int? PuppiesCount,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
