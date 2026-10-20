using System.ComponentModel.DataAnnotations;
using aRevolucaoDosPet.Enums;

namespace aRevolucaoDosPet.DTOs;

public class AppointmentAttachmentDto
{
    [Required]
    [Url]
    [StringLength(2048)]
    public string Url { get; set; } = string.Empty;

    [StringLength(255)]
    public string? PublicId { get; set; }

    [Required]
    [StringLength(30)]
    public string Type { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Name { get; set; }
}

public class AppointmentWriteRequest
{
    [EnumDataType(typeof(AppointmentType))]
    public required AppointmentType Type { get; init; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(5000)]
    public string? Description { get; set; }

    public required DateTime ScheduledAt { get; init; }

    [Range(1, 1440)]
    public int? DurationMinutes { get; set; }

    [EnumDataType(typeof(AppointmentStatus))]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.SCHEDULED;

    [StringLength(150)]
    public string? VeterinarianName { get; set; }

    [StringLength(30)]
    public string? VeterinarianCrmv { get; set; }

    [StringLength(150)]
    public string? ClinicName { get; set; }

    [StringLength(300)]
    public string? ClinicAddress { get; set; }

    [Range(0, 1_000_000)]
    public decimal? Price { get; set; }

    [StringLength(5000)]
    public string? Diagnosis { get; set; }

    [Url]
    [StringLength(2048)]
    public string? PrescriptionUrl { get; set; }

    [MaxLength(10)]
    public List<AppointmentAttachmentDto> Attachments { get; set; } = [];
}

public class CreateAppointmentRequest : AppointmentWriteRequest
{
    [Required]
    public string PetId { get; set; } = string.Empty;
}

public class AppointmentSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? PetId { get; set; }
    public AppointmentType? Type { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public AppointmentStatus? Status { get; set; }
    public DateTime? ScheduledAtFrom { get; set; }
    public DateTime? ScheduledAtTo { get; set; }

    [Range(1, 1440)]
    public int? MinDurationMinutes { get; set; }

    [Range(1, 1440)]
    public int? MaxDurationMinutes { get; set; }

    public string? VeterinarianName { get; set; }
    public string? VeterinarianCrmv { get; set; }
    public string? ClinicName { get; set; }
    public string? ClinicAddress { get; set; }

    [Range(0, 1_000_000)]
    public decimal? MinPrice { get; set; }

    [Range(0, 1_000_000)]
    public decimal? MaxPrice { get; set; }

    public string? Diagnosis { get; set; }
    public bool? HasPrescription { get; set; }
    public bool? HasAttachments { get; set; }
    public bool? ReminderSent { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? UpdatedAtFrom { get; set; }
    public DateTime? UpdatedAtTo { get; set; }
}

public record AppointmentResponseDto(
    string Id,
    string PetId,
    AppointmentType Type,
    string Title,
    string? Description,
    DateTime ScheduledAt,
    int? DurationMinutes,
    AppointmentStatus Status,
    string? VeterinarianName,
    string? VeterinarianCrmv,
    string? ClinicName,
    string? ClinicAddress,
    decimal? Price,
    string? Diagnosis,
    string? PrescriptionUrl,
    IReadOnlyList<AppointmentAttachmentDto> Attachments,
    bool ReminderSent,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
