using System.ComponentModel.DataAnnotations;
using aRevolucaoDosPet.Enums;

namespace aRevolucaoDosPet.DTOs;

public class HeatCycleWriteRequest
{
    public required DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; set; }

    [EnumDataType(typeof(HeatCyclePhase))]
    public HeatCyclePhase? Phase { get; set; }

    [MaxLength(30)]
    public List<string> Symptoms { get; set; } = [];

    [StringLength(5000)]
    public string? Notes { get; set; }

    public DateTime? PredictedNextAt { get; set; }
}

public class CreateHeatCycleRequest : HeatCycleWriteRequest
{
    [Required]
    public string PetId { get; set; } = string.Empty;
}

public class HeatCycleSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? PetId { get; set; }
    public HeatCyclePhase? Phase { get; set; }
    public string? Symptom { get; set; }
    public string? Notes { get; set; }
    public DateTime? StartedAtFrom { get; set; }
    public DateTime? StartedAtTo { get; set; }
    public DateTime? EndedAtFrom { get; set; }
    public DateTime? EndedAtTo { get; set; }
    public DateTime? PredictedNextAtFrom { get; set; }
    public DateTime? PredictedNextAtTo { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? UpdatedAtFrom { get; set; }
    public DateTime? UpdatedAtTo { get; set; }
}

public record HeatCycleResponseDto(
    string Id,
    string PetId,
    DateTime StartedAt,
    DateTime? EndedAt,
    HeatCyclePhase? Phase,
    IReadOnlyList<string> Symptoms,
    string? Notes,
    DateTime? PredictedNextAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
