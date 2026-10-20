using System.ComponentModel.DataAnnotations;
using aRevolucaoDosPet.Enums;

namespace aRevolucaoDosPet.DTOs;

public class VaccineWriteRequest
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Laboratory { get; set; }

    [StringLength(100)]
    public string? BatchNumber { get; set; }

    [Range(1, 20)]
    public int? DoseNumber { get; set; }

    public DateTime? AppliedAt { get; set; }
    public DateTime? NextDoseAt { get; set; }

    [StringLength(150)]
    public string? VeterinarianName { get; set; }

    [StringLength(30)]
    public string? VeterinarianCrmv { get; set; }

    [StringLength(150)]
    public string? ClinicName { get; set; }

    [Url]
    [StringLength(2048)]
    public string? CertificateUrl { get; set; }

    [EnumDataType(typeof(VaccineStatus))]
    public VaccineStatus Status { get; set; } = VaccineStatus.APPLIED;
}

public class CreateVaccineRequest : VaccineWriteRequest
{
    [Required]
    public string PetId { get; set; } = string.Empty;
}

public class VaccineSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? PetId { get; set; }
    public string? Name { get; set; }
    public string? Laboratory { get; set; }
    public string? BatchNumber { get; set; }

    [Range(1, 20)]
    public int? MinDoseNumber { get; set; }

    [Range(1, 20)]
    public int? MaxDoseNumber { get; set; }

    public string? VeterinarianName { get; set; }
    public string? VeterinarianCrmv { get; set; }
    public string? ClinicName { get; set; }
    public bool? HasCertificate { get; set; }
    public VaccineStatus? Status { get; set; }
    public DateTime? AppliedAtFrom { get; set; }
    public DateTime? AppliedAtTo { get; set; }
    public DateTime? NextDoseAtFrom { get; set; }
    public DateTime? NextDoseAtTo { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? UpdatedAtFrom { get; set; }
    public DateTime? UpdatedAtTo { get; set; }
}

public record VaccineResponseDto(
    string Id,
    string PetId,
    string Name,
    string? Laboratory,
    string? BatchNumber,
    int? DoseNumber,
    DateTime? AppliedAt,
    DateTime? NextDoseAt,
    string? VeterinarianName,
    string? VeterinarianCrmv,
    string? ClinicName,
    string? CertificateUrl,
    VaccineStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
